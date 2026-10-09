using System;
using System.Drawing.Imaging;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Orion.Client.Helper.ScreenStuff.DesktopDuplication
{
    /// <summary>
    /// Provides access to frame-by-frame updates of a particular desktop (i.e. one monitor), with image and cursor information.
    /// </summary>
    public class DesktopDuplicator : IDisposable
    {
        /// <summary>
        /// Represents a mapped desktop frame backed by GPU staging texture memory.
        /// Dispose when done to unmap and release the frame.
        /// </summary>
        public sealed class MappedFrame : IDisposable
        {
            public IntPtr DataPointer { get; internal set; }
            public int RowPitch { get; internal set; }
            public int Width { get; internal set; }
            public int Height { get; internal set; }
            public System.Drawing.Imaging.PixelFormat PixelFormat { get; internal set; }

            private Action _onDispose;

            internal MappedFrame(Action onDispose)
            {
                _onDispose = onDispose;
            }

            public void Dispose()
            {
                var action = System.Threading.Interlocked.Exchange(ref _onDispose, null);
                action?.Invoke();
            }
        }

        private Device mDevice;
        private Texture2DDescription mTextureDesc;
        private OutputDescription mOutputDesc;
        private OutputDuplication mDeskDupl;

        private Texture2D desktopImageTexture = null;
        private OutputDuplicateFrameInformation frameInfo = new OutputDuplicateFrameInformation();
        private int mWhichOutputDevice = -1;

        // Reuse bitmap to avoid allocations
        private Bitmap reusableBitmap;
        private int cachedWidth, cachedHeight;
        private bool disposed = false;

        /// <summary>
        /// Duplicates the output of the specified monitor.
        /// </summary>
        /// <param name="whichMonitor">The output device to duplicate (i.e. monitor). Begins with zero, which seems to correspond to the primary monitor.</param>
        public DesktopDuplicator(int whichMonitor)
            : this(0, whichMonitor) { }

        /// <summary>
        /// Duplicates the output of the specified monitor on the specified graphics adapter.
        /// </summary>
        /// <param name="whichGraphicsCardAdapter">The adapter which contains the desired outputs.</param>
        /// <param name="whichOutputDevice">The output device to duplicate (i.e. monitor). Begins with zero, which seems to correspond to the primary monitor.</param>
        public DesktopDuplicator(int whichGraphicsCardAdapter, int whichOutputDevice)
        {
            this.mWhichOutputDevice = whichOutputDevice;
            Adapter1 adapter = null;
            try
            {
                adapter = new Factory1().GetAdapter1(whichGraphicsCardAdapter);
            }
            catch (SharpDXException)
            {
                throw new DesktopDuplicationException("Could not find the specified graphics card adapter.");
            }
            this.mDevice = new Device(adapter);
            Output output = null;
            try
            {
                output = adapter.GetOutput(whichOutputDevice);
            }
            catch (SharpDXException)
            {
                throw new DesktopDuplicationException("Could not find the specified output device.");
            }
            var output1 = output.QueryInterface<Output1>();
            this.mOutputDesc = output.Description;
            this.mTextureDesc = new Texture2DDescription()
            {
                CpuAccessFlags = CpuAccessFlags.Read,
                BindFlags = BindFlags.None,
                Format = Format.B8G8R8A8_UNorm,
                Width = this.mOutputDesc.DesktopBounds.Right - this.mOutputDesc.DesktopBounds.Left,
                Height = this.mOutputDesc.DesktopBounds.Bottom - this.mOutputDesc.DesktopBounds.Top,
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = { Count = 1, Quality = 0 },
                Usage = ResourceUsage.Staging
            };

            try
            {
                this.mDeskDupl = output1.DuplicateOutput(mDevice);
            }
            catch (SharpDXException ex)
            {
                if (ex.ResultCode.Code == SharpDX.DXGI.ResultCode.NotCurrentlyAvailable.Result.Code)
                {
                    throw new DesktopDuplicationException("There is already the maximum number of applications using the Desktop Duplication API running, please close one of the applications and try again.");
                }
                throw new DesktopDuplicationException("Failed to duplicate output.", ex);
            }
            finally
            {
                output1?.Dispose();
                output?.Dispose();
                adapter?.Dispose();
            }
        }

        /// <summary>
        /// Attempts to acquire and map the latest desktop frame for zero-copy access.
        /// Returns false on timeout (no new frame); otherwise, returns a disposable mapped frame.
        /// </summary>
        public bool TryGetLatestMappedFrame(out MappedFrame mapped)
        {
            mapped = null;
            if (disposed)
                throw new ObjectDisposedException(nameof(DesktopDuplicator));

            if (desktopImageTexture == null)
                desktopImageTexture = new Texture2D(mDevice, mTextureDesc);

            SharpDX.DXGI.Resource desktopResource = null;
            frameInfo = new OutputDuplicateFrameInformation();
            try
            {
                // fml
                mDeskDupl.AcquireNextFrame(33, out frameInfo, out desktopResource);
            }
            catch (SharpDXException ex)
            {
                if (ex.ResultCode.Code == SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code)
                {
                    return false; // no new frame
                }
                if (ex.ResultCode.Failure)
                {
                    throw new DesktopDuplicationException("Failed to acquire next frame.");
                }
            }

            using (var tempTexture = desktopResource.QueryInterface<Texture2D>())
            {
                mDevice.ImmediateContext.CopyResource(tempTexture, desktopImageTexture);
            }
            desktopResource.Dispose();

            var mapSource = mDevice.ImmediateContext.MapSubresource(desktopImageTexture, 0, MapMode.Read, MapFlags.None);

            int width = mOutputDesc.DesktopBounds.Right - mOutputDesc.DesktopBounds.Left;
            int height = mOutputDesc.DesktopBounds.Bottom - mOutputDesc.DesktopBounds.Top;

            var mf = new MappedFrame(() =>
            {
                try
                {
                    mDevice.ImmediateContext.UnmapSubresource(desktopImageTexture, 0);
                }
                finally
                {
                    try { mDeskDupl.ReleaseFrame(); } catch { }
                }
            })
            {
                DataPointer = mapSource.DataPointer,
                RowPitch = mapSource.RowPitch,
                Width = width,
                Height = height,
                PixelFormat = System.Drawing.Imaging.PixelFormat.Format32bppArgb
            };

            mapped = mf;
            return true;
        }

        /// <summary>
        /// Retrieves the latest desktop image and associated metadata.
        /// </summary>
        public DesktopFrame GetLatestFrame()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(DesktopDuplicator));

            var frame = new DesktopFrame();
            bool retrievalTimedOut = RetrieveFrame();
            if (retrievalTimedOut)
                return null;
            try
            {
                RetrieveFrameMetadata(frame);
                RetrieveCursorMetadata(frame);
                ProcessFrame(frame);
            }
            catch
            {
                ReleaseFrame();
                throw;
            }
            try
            {
                ReleaseFrame();
            }
            catch { 
            }
            return frame;
        }

        private bool RetrieveFrame()
        {
            if (desktopImageTexture == null)
                desktopImageTexture = new Texture2D(mDevice, mTextureDesc);
            
            SharpDX.DXGI.Resource desktopResource = null;
            frameInfo = new OutputDuplicateFrameInformation();
            try
            {
                mDeskDupl.AcquireNextFrame(33, out frameInfo, out desktopResource);
            }
            catch (SharpDXException ex)
            {
                if (ex.ResultCode.Code == SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code)
                {
                    return true;
                }
                if (ex.ResultCode.Failure)
                {
                    throw new DesktopDuplicationException("Failed to acquire next frame.");
                }
            }
            using (var tempTexture = desktopResource.QueryInterface<Texture2D>())
                mDevice.ImmediateContext.CopyResource(tempTexture, desktopImageTexture);
            desktopResource.Dispose();
            return false;
        }

        private void RetrieveFrameMetadata(DesktopFrame frame)
        {
            frame.MovedRegions = new MovedRegion[0];
            frame.UpdatedRegions = new System.Drawing.Rectangle[0];
        }

        private void RetrieveCursorMetadata(DesktopFrame frame)
        {
            frame.CursorLocation = new System.Drawing.Point(0, 0);
            frame.CursorVisible = false;
        }
        
        private void ProcessFrame(DesktopFrame frame)
        {
            var mapSource = mDevice.ImmediateContext.MapSubresource(desktopImageTexture, 0, MapMode.Read, MapFlags.None);

            int width = mOutputDesc.DesktopBounds.Right - mOutputDesc.DesktopBounds.Left;
            int height = mOutputDesc.DesktopBounds.Bottom - mOutputDesc.DesktopBounds.Top;

            if (reusableBitmap == null || cachedWidth != width || cachedHeight != height)
            {
                reusableBitmap?.Dispose();
                reusableBitmap = new System.Drawing.Bitmap(width, height, PixelFormat.Format32bppArgb);
                cachedWidth = width;
                cachedHeight = height;
            }

            var boundsRect = new System.Drawing.Rectangle(0, 0, width, height);
            
            var mapDest = reusableBitmap.LockBits(boundsRect, ImageLockMode.WriteOnly, reusableBitmap.PixelFormat);
            var sourcePtr = mapSource.DataPointer;
            var destPtr = mapDest.Scan0;
            
            int sourceStride = mapSource.RowPitch;
            int destStride = mapDest.Stride;
            int copyWidth = Math.Min(width * 4, destStride);
            
            unsafe
            {
                byte* src = (byte*)sourcePtr.ToPointer();
                byte* dst = (byte*)destPtr.ToPointer();
                
                for (int y = 0; y < height; y++)
                {
                    System.Buffer.MemoryCopy(src, dst, destStride, copyWidth);
                    
                    src += sourceStride;
                    dst += destStride;
                }
            }

            reusableBitmap.UnlockBits(mapDest);
            mDevice.ImmediateContext.UnmapSubresource(desktopImageTexture, 0);
            
            frame.DesktopImage = new Bitmap(reusableBitmap);
        }

        private void ReleaseFrame()
        {
            try
            {
                mDeskDupl.ReleaseFrame();
            }
            catch (SharpDXException ex)
            {
                if (ex.ResultCode.Failure)
                {
                    throw new DesktopDuplicationException("Failed to release frame.");
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    reusableBitmap?.Dispose();
                    desktopImageTexture?.Dispose();
                    mDeskDupl?.Dispose();
                    mDevice?.Dispose();
                }

                reusableBitmap = null;
                desktopImageTexture = null;
                mDeskDupl = null;
                mDevice = null;

                disposed = true;
            }
        }

        ~DesktopDuplicator()
        {
            Dispose(false);
        }
    }
}
