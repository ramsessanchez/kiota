using System;
using System.Linq;
using System.Text;


namespace Kiota.Builder.Extensions {
    public static class StringExtensions {
        public static string ToFirstCharacterLowerCase(this string input)
            => string.IsNullOrEmpty(input) ? input : $"{char.ToLowerInvariant(input.FirstOrDefault())}{input.Substring(1)}";
        public static string ToFirstCharacterUpperCase(this string input)
            => string.IsNullOrEmpty(input) ? input : Char.ToUpperInvariant(input.FirstOrDefault()) + input.Substring(1);
        public static string ToCamelCase(this string name)
        {
            if(string.IsNullOrEmpty(name)) return name;
            var chunks = name.Split("-", StringSplitOptions.RemoveEmptyEntries);
            var identifier = String.Join(null, chunks.Take(1)
                                                  .Union(chunks.Skip(1)
                                                                .Select(s => ToFirstCharacterUpperCase(s))));
            return identifier;
        }
        public static string ToPascalCase(this string name)
            => string.IsNullOrEmpty(name) ? name : String.Join(null, name.Split("-", StringSplitOptions.RemoveEmptyEntries)
                                                                            .Select(s => ToFirstCharacterUpperCase(s)));
        public static string ReplaceValueIdentifier(this string original) =>
            original?.Replace("$value", "Content");
        
        public static string ToSnakeCase(this string name)
        {
            if(string.IsNullOrEmpty(name)) return name;
            var chunks = name.Split("-", StringSplitOptions.RemoveEmptyEntries);
            var identifier = String.Join("", chunks.Take(1)
                                                  .Union(chunks.Skip(1)
                                                                .Select(s => ToFirstCharacterLowerCase(s))));
            if(identifier.Length < 2) {
                return identifier;
            }
            var sb = new StringBuilder();
            sb.Append(char.ToLowerInvariant(identifier[0]));
            for(int i = 1; i < identifier.Length; ++i) {
                char c = identifier[i];
                if(char.IsUpper(c)) {
                    sb.Append('_');
                    sb.Append(char.ToLowerInvariant(c));
                } else {
                    sb.Append(c);
                }
            }
            var output = sb.ToString();
            int index = output.IndexOf("<");
            if (index >= 0)
                output = output.Substring(0, index);
            
            return output.ToString();
        }
    }
}
