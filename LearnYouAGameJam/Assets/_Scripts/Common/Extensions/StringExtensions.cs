using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LYGJ.Common {
    public static class StringExtensions {
        static readonly Regex _PascalCamelCaseRegex = new("(^[a-z]+|[A-Z]+(?![a-z])|[A-Z][a-z]+)", RegexOptions.Compiled);

        /// <summary> Determines the most likely naming convention of a given string. </summary>
        /// <param name="S"> The string to analyse. </param>
        /// <returns> The most likely naming convention of the string. </returns>
        /// <exception cref="ArgumentException"> Thrown if the string is null or empty, or if the naming convention cannot be determined. </exception>
        public static NamingConvention GetNamingConvention( this string S ) {
            if (string.IsNullOrEmpty(S)) {
                throw new ArgumentException("String cannot be null or empty.", nameof(S));
            }

            if (S.Contains("_")) {
                return S == S.ToUpper() ? NamingConvention.UpperCaseWithUnderscores : NamingConvention.SnakeCase;
            }

            if (S.Contains("-")) {
                return NamingConvention.KebabCase;
            }

            if (S == S.ToLower()) {
                return NamingConvention.LowerCase;
            }

            if (S == S.ToUpper()) {
                return NamingConvention.UpperCase;
            }

            string[] SplitString = S.Split();
            if (SplitString.Length > 1) {
                string FirstWord = SplitString[0];
                if (FirstWord.Length > 1 && char.IsUpper(SplitString[0][0]) && char.IsLower(SplitString[0][1])) {
                    string SecondWord = SplitString[1];
                    if (SecondWord.Length > 1 && char.IsUpper(SecondWord[0]) && char.IsLower(SecondWord[1])) {
                        return NamingConvention.TitleCase;
                    }
                }

                return NamingConvention.SentenceCase;
            }

            if (S.Any(char.IsLower) && S.Any(char.IsUpper)) {
                return char.IsUpper(S[0]) ? NamingConvention.PascalCase : NamingConvention.CamelCase;
            }

            throw new ArgumentException("Unable to determine the naming convention of the given string.", nameof(S));
        }

        /// <summary> Converts a string to a given naming convention. </summary>
        /// <param name="S"> The string to convert. </param>
        /// <param name="In"> The naming convention of the string. </param>
        /// <param name="Out"> The naming convention to convert the string to. </param>
        /// <returns> The converted string. </returns>
        public static string ConvertNamingConvention( this string S, NamingConvention In, NamingConvention Out ) {
            List<string> Words = new();
            switch (In) {
                case NamingConvention.SnakeCase:
                case NamingConvention.UpperCaseWithUnderscores:
                    Words.AddRange(S.Split('_'));
                    break;
                case NamingConvention.KebabCase:
                    Words.AddRange(S.Split('-'));
                    break;
                case NamingConvention.CamelCase:
                case NamingConvention.PascalCase:
                    int WordStartIndex = 0;
                    for (int I = 0; I < S.Length; I++) {
                        if (I > 0 && char.IsUpper(S[I])) {
                            Words.Add(S[WordStartIndex..I]);
                            WordStartIndex = I;
                        }
                    }

                    Words.Add(S[WordStartIndex..]);
                    break;
                case NamingConvention.TitleCase:
                case NamingConvention.SentenceCase:
                    Words.AddRange(S.Split(' '));
                    break;
            }

            StringBuilder Result = new();
            for (int I = 0; I < Words.Count; I++) {
                string? Word        = Words[I];
                bool    IsFirstWord = I == 0;

                switch (Out) {
                    case NamingConvention.SnakeCase:
                    case NamingConvention.KebabCase:
                    case NamingConvention.LowerCase:
                        Word = Word.ToLower();
                        break;
                    case NamingConvention.PascalCase:
                    case NamingConvention.TitleCase:
                        Word = char.ToUpper(Word[0]) + Word[1..].ToLower();
                        break;
                    case NamingConvention.CamelCase:
                        Word = IsFirstWord ? Word.ToLower() : char.ToUpper(Word[0]) + Word[1..].ToLower();
                        break;
                    case NamingConvention.SentenceCase:
                        Word = IsFirstWord ? char.ToUpper(Word[0]) + Word[1..].ToLower() : Word.ToLower();
                        break;
                    case NamingConvention.UpperCase:
                    case NamingConvention.UpperCaseWithUnderscores:
                        Word = Word.ToUpper();
                        break;
                }

                switch (Out) {
                    case NamingConvention.SnakeCase:
                    case NamingConvention.UpperCaseWithUnderscores:
                        if (!IsFirstWord) {
                            Result.Append('_');
                        }

                        Result.Append(Word);
                        break;
                    case NamingConvention.KebabCase:
                        if (!IsFirstWord) {
                            Result.Append('-');
                        }

                        Result.Append(Word);
                        break;
                    case NamingConvention.PascalCase:
                    case NamingConvention.CamelCase:
                    case NamingConvention.LowerCase:
                    case NamingConvention.UpperCase:
                        Result.Append(Word);
                        break;
                    case NamingConvention.TitleCase:
                    case NamingConvention.SentenceCase:
                        if (!IsFirstWord) {
                            Result.Append(' ');
                        }

                        Result.Append(Word);
                        break;
                }
            }

            return Result.ToString();
        }

        /// <inheritdoc cref="ConvertNamingConvention(string,NamingConvention,NamingConvention)"/>
        public static string ConvertNamingConvention( this string S, NamingConvention Out ) => ConvertNamingConvention(S, GetNamingConvention(S), Out);

    }

    // ReSharper disable CommentTypo
    /// <summary>
    /// An enumeration of common naming conventions.
    /// </summary>
    public enum NamingConvention {
        /// <summary> Snake case (e.g., my_variable_name). </summary>
        SnakeCase,
        /// <summary> Pascal case (e.g., MyVariableName). </summary>
        PascalCase,
        /// <summary> Camel case (e.g., myVariableName). </summary>
        CamelCase,
        /// <summary> Kebab case (e.g., my-variable-name). </summary>
        KebabCase,
        /// <summary> Upper case with underscores (e.g., MY_VARIABLE_NAME). </summary>
        UpperCaseWithUnderscores,
        /// <summary> Lower case (e.g., myvariablename). </summary>
        LowerCase,
        /// <summary> Upper case (e.g., MYVARIABLENAME). </summary>
        UpperCase,
        /// <summary> Title case (e.g., My Variable Name). </summary>
        TitleCase,
        /// <summary> Sentence case (e.g., My variable name). </summary>
        SentenceCase
    }
    // ReSharper restore CommentTypo
}
