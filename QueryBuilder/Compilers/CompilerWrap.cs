using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SqlKata.Compilers
{
    internal class CompilerWrap
    {
        public virtual string parameterPlaceholder { get; set; } = "?";
        public virtual string parameterPrefix { get; set; } = "@p";
        public virtual string OpeningIdentifier { get; set; } = "\"";
        public virtual string ClosingIdentifier { get; set; } = "\"";
        public virtual string ColumnAsKeyword { get; set; } = "AS ";
        public virtual string TableAsKeyword { get; set; } = "AS ";
        public virtual string LastId { get; set; } = "";
        public virtual string EscapeCharacter { get; set; } = "\\";
        public virtual string WrapIdentifiers(string input)
        {
            return input

                // deprecated
                .ReplaceIdentifierUnlessEscaped(this.EscapeCharacter, "{", this.OpeningIdentifier)
                .ReplaceIdentifierUnlessEscaped(this.EscapeCharacter, "}", this.ClosingIdentifier)

                .ReplaceIdentifierUnlessEscaped(this.EscapeCharacter, "[", this.OpeningIdentifier)
                .ReplaceIdentifierUnlessEscaped(this.EscapeCharacter, "]", this.ClosingIdentifier);
        }
        /// <summary>
        /// Wrap a single string in a column identifier.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual string Wrap(string value)
        {

            if (value.ToLowerInvariant().Contains(" as "))
            {
                int index = value.ToLowerInvariant().IndexOf(" as ");
                string before = value.Substring(0, index);
                string after = value.Substring(index + 4);

                return Wrap(before) + $" {ColumnAsKeyword}" + WrapValue(after);
            }

            if (value.Contains("."))
            {
                return string.Join(".", value.Split('.').Select((x, index) =>
                {
                    return WrapValue(x);
                }));
            }

            // If we reach here then the value does not contain an "AS" alias
            // nor dot "." expression, so wrap it as regular value.
            return WrapValue(value);
        }
        /// <summary>
        /// Wrap a single string in keyword identifiers.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual string WrapValue(string value)
        {
            if (value == "*") return value;

            string opening = this.OpeningIdentifier;
            string closing = this.ClosingIdentifier;

            return opening + value.Replace(closing, closing + closing) + closing;
        }
    }
    internal class FireBirdWrap : CompilerWrap
    {
        public override string WrapValue(string value)
        {
            return base.WrapValue(value).ToUpperInvariant();
        }
    }
    internal class SqlLiteWrap : CompilerWrap
    {
        public override string parameterPlaceholder { get; set; } = "?";
        public override string parameterPrefix { get; set; } = "@p";
        public override string OpeningIdentifier { get; set; } = "\"";
        public override string ClosingIdentifier { get; set; } = "\"";
        public override string LastId { get; set; } = "select last_insert_rowid() as id";

    }
}
