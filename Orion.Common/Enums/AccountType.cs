namespace Orion.Common.Enums
{
    public enum AccountType
    {
        Admin,
        User,
        Guest,
        Unknown,
        /// <summary>
        /// NT AUTHORITY\SYSTEM（令牌为 S-1-5-18）。由内置「提权至 SYSTEM」克隆 winlogon 令牌拉起的新 beacon 会以此身份上线。
        /// </summary>
        System
    }
}
