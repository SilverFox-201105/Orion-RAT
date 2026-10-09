using Mono.Cecil;
using Orion.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Orion.Server.Build
{
    public class Renamer
    {
        public AssemblyDefinition AsmDef { get; set; }

        private int MinLength { get; set; }
        private int MaxLength { get; set; }
        private MemberOverloader _typeOverloader;
        private Dictionary<TypeDefinition, MemberOverloader> _methodOverloaders;
        private Dictionary<TypeDefinition, MemberOverloader> _fieldOverloaders;
        private Dictionary<TypeDefinition, MemberOverloader> _eventOverloaders;
        private Dictionary<string, string> _namespaceRenames;

        private readonly SafeRandom _random = new SafeRandom();

        public Renamer(AssemblyDefinition asmDef)
            : this(asmDef, 10, 30)
        {
        }

        public Renamer(AssemblyDefinition asmDef, int minLength, int maxLength)
        {
            this.AsmDef = asmDef;
            this.MinLength = minLength;
            this.MaxLength = maxLength;
            _typeOverloader = new MemberOverloader(this.MinLength, this.MaxLength);
            _methodOverloaders = new Dictionary<TypeDefinition, MemberOverloader>();
            _fieldOverloaders = new Dictionary<TypeDefinition, MemberOverloader>();
            _eventOverloaders = new Dictionary<TypeDefinition, MemberOverloader>();
            _namespaceRenames = new Dictionary<string, string>();
        }

        public bool Perform()
        {
            try
            {
                foreach (TypeDefinition typeDef in AsmDef.Modules.SelectMany(module => module.Types))
                {
                    RenameInType(typeDef);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void RenameInType(TypeDefinition typeDef)
        {
            if (!typeDef.Namespace.StartsWith("Orion") ||
                typeDef.Namespace.Contains(".Messages") ||
                typeDef.IsEnum)
                return;

            if (IsCompilerGeneratedType(typeDef))
                return;

            if (HasCompilerGeneratedNestedType(typeDef))
                return;

            typeDef.Namespace = RenameNamespace(typeDef.Namespace);

            _typeOverloader.GiveName(typeDef);

            MemberOverloader methodOverloader = GetMethodOverloader(typeDef);
            MemberOverloader fieldOverloader = GetFieldOverloader(typeDef);
            MemberOverloader eventOverloader = GetEventOverloader(typeDef);

            if (typeDef.HasNestedTypes)
                foreach (TypeDefinition nestedType in typeDef.NestedTypes)
                    RenameInType(nestedType);

            if (typeDef.HasMethods)
                foreach (MethodDefinition methodDef in
                        typeDef.Methods.Where(methodDef =>
                                !methodDef.IsConstructor && !methodDef.HasCustomAttributes &&
                                !methodDef.IsAbstract && !methodDef.IsVirtual &&
                                !IsCompilerGeneratedMember(methodDef.Name)))
                    methodOverloader.GiveName(methodDef);

            if (typeDef.HasFields)
                foreach (FieldDefinition fieldDef in
                        typeDef.Fields.Where(fieldDef => !IsCompilerGeneratedMember(fieldDef.Name)))
                    fieldOverloader.GiveName(fieldDef);

            if (typeDef.HasProperties)
                foreach (PropertyDefinition propDef in
                        typeDef.Properties.Where(propDef => !IsCompilerGeneratedMember(propDef.Name)))
                {
                }

            if (typeDef.HasEvents)
                foreach (EventDefinition eventDef in
                        typeDef.Events.Where(eventDef => !IsCompilerGeneratedMember(eventDef.Name)))
                    eventOverloader.GiveName(eventDef);
        }

        private static bool IsCompilerGeneratedType(TypeDefinition typeDef)
        {
            if (typeDef == null) return false;

            if (typeDef.HasCustomAttributes)
            {
                foreach (var ca in typeDef.CustomAttributes)
                {
                    if (ca.AttributeType != null &&
                        (ca.AttributeType.Name == "CompilerGeneratedAttribute" ||
                         ca.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute"))
                        return true;
                }
            }

            string name = typeDef.Name ?? string.Empty;
            if (name.StartsWith("<>") || name.StartsWith("<") && name.Contains(">"))
                return true;
            if (name.Contains("<>c__DisplayClass") || name.Contains("<>c__AnonStorey") ||
                name.Contains("<>f__AnonymousType") || name.Contains("<>d__") ||
                name.Contains("<>e__") || name.Contains("<>g__"))
                return true;
            if (name.StartsWith("VB$") || name.Contains("$VB$"))
                return true;

            if (typeDef.IsNested && typeDef.DeclaringType != null)
                return IsCompilerGeneratedType(typeDef.DeclaringType);

            return false;
        }

        private static bool IsCompilerGeneratedMember(string memberName)
        {
            if (string.IsNullOrEmpty(memberName)) return false;
            if (memberName.StartsWith("<"))
                return true;
            if (memberName.Contains("k__BackingField") || memberName.Contains("__BackingField"))
                return true;
            if (memberName.StartsWith("add_") || memberName.StartsWith("remove_"))
                return false;
            if (memberName.StartsWith("get_") || memberName.StartsWith("set_"))
                return false;
            return false;
        }

        private static bool HasCompilerGeneratedNestedType(TypeDefinition typeDef)
        {
            if (typeDef == null) return false;
            if (!typeDef.HasNestedTypes) return false;

            foreach (var nested in typeDef.NestedTypes)
            {
                if (IsCompilerGeneratedType(nested))
                    return true;
                if (HasCompilerGeneratedNestedType(nested))
                    return true;
            }
            return false;
        }

        private string RenameNamespace(string originalNamespace)
        {
            if (string.IsNullOrEmpty(originalNamespace))
                return originalNamespace;

            if (!_namespaceRenames.TryGetValue(originalNamespace, out string newNamespace))
            {
                newNamespace = GenerateRandomNamespace();
                _namespaceRenames[originalNamespace] = newNamespace;
            }

            return newNamespace;
        }

        private string GenerateRandomNamespace()
        {
            StringBuilder builder = new StringBuilder();
            int length = _random.Next(MinLength, MaxLength);
            for (int i = 0; i < length; i++)
            {
                builder.Append((char)_random.Next('a', 'z' + 1));
            }
            return builder.ToString();
        }

        private MemberOverloader GetMethodOverloader(TypeDefinition typeDef)
        {
            return GetOverloader(this._methodOverloaders, typeDef);
        }

        private MemberOverloader GetFieldOverloader(TypeDefinition typeDef)
        {
            return GetOverloader(this._fieldOverloaders, typeDef);
        }

        private MemberOverloader GetEventOverloader(TypeDefinition typeDef)
        {
            return GetOverloader(this._eventOverloaders, typeDef);
        }

        private MemberOverloader GetOverloader(Dictionary<TypeDefinition, MemberOverloader> overloaderDictionary,
            TypeDefinition targetTypeDef)
        {
            if (!overloaderDictionary.TryGetValue(targetTypeDef, out MemberOverloader overloader))
            {
                overloader = new MemberOverloader(this.MinLength, this.MaxLength);
                overloaderDictionary.Add(targetTypeDef, overloader);
            }
            return overloader;
        }

        private class MemberOverloader
        {
            private bool DoRandom { get; set; }
            private int MinLength { get; set; }
            private int MaxLength { get; set; }
            private readonly Dictionary<string, string> _renamedMembers = new Dictionary<string, string>();
            private readonly char[] _charMap;
            private readonly SafeRandom _random = new SafeRandom();
            private int[] _indices;

            public MemberOverloader(int minLength, int maxLength, bool doRandom = true)
                : this(minLength, maxLength, doRandom, "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray())
            {
            }

            private MemberOverloader(int minLength, int maxLength, bool doRandom, char[] chars)
            {
                this._charMap = chars;
                this.DoRandom = doRandom;
                this.MinLength = minLength;
                this.MaxLength = maxLength;
                this._indices = new int[minLength];
            }

            public void GiveName(MemberReference member)
            {
                string currentName = GetCurrentName();
                string originalName = member.ToString();
                member.Name = currentName;
                while (_renamedMembers.ContainsValue(member.ToString()))
                {
                    member.Name = GetCurrentName();
                }
                _renamedMembers.Add(originalName, member.ToString());
            }

            private string GetCurrentName()
            {
                return DoRandom ? GetRandomName() : GetOverloadedName();
            }

            private string GetRandomName()
            {
                StringBuilder builder = new StringBuilder();
                int length = _random.Next(MinLength, MaxLength);

                for (int i = 0; i < length; i++)
                {
                    builder.Append(_charMap[_random.Next(_charMap.Length)]);
                }

                return builder.ToString();
            }

            private string GetOverloadedName()
            {
                IncrementIndices();
                char[] chars = new char[_indices.Length];
                for (int i = 0; i < _indices.Length; i++)
                    chars[i] = _charMap[_indices[i]];
                return new string(chars);
            }

            private void IncrementIndices()
            {
                for (int i = _indices.Length - 1; i >= 0; i--)
                {
                    _indices[i]++;
                    if (_indices[i] >= _charMap.Length)
                    {
                        if (i == 0)
                            Array.Resize(ref _indices, _indices.Length + 1);
                        _indices[i] = 0;
                    }
                    else
                        break;
                }
            }
        }
    }
}