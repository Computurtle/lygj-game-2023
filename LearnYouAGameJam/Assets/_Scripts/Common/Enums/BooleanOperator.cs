using System.Diagnostics;

namespace LYGJ.Common.Enums {

    public enum BooleanOperator {
        /// <summary> The logical AND operator (<c>&amp;&amp;</c>). </summary>
        And,
        /// <summary> The logical OR operator (<c>||</c>). </summary>
        Or,
        /// <summary> The logical XOR operator (<c>^</c>). </summary>
        Xor,
        /// <summary> The logical NAND operator (<c>!(&amp;&amp;)</c>). </summary>
        Nand,
        /// <summary> The logical NOR operator (<c>!(||)</c>). </summary>
        Nor,
        /// <summary> The logical XNOR operator (<c>!(^)</c>). </summary>
        Xnor,
        /// <summary> The left-only operator (<c>&amp;&amp; !</c>). </summary>
        LeftOnly,
        /// <summary> The right-only operator (<c>! &amp;&amp;</c>). </summary>
        RightOnly
    }
}
