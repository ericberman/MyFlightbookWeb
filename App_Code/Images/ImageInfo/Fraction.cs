using System;
using System.Globalization;

namespace gma.Drawing.ImageInfo
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]
    public struct FractionStruct
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public Int32 num;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public Int32 den;
    }

	///<summary>Some values according EXIF specifications are stored as Fraction numbers.
 	///This Class helps to manipulate and display those numbers.</summary>
	public class Fraction 
	{
   		Int32 num = 0;
   		Int32 den = 1;
		///<summary>Creates a Fraction Number having Numerator and Denumerator.</summary>
		///<param name="num">Numerator</param>
		///<param name="den">Denumerator</param>
   		public Fraction(int num, int den) 
   		{
      		this.num = num;
      		this.den = den;
   		}
		///<summary>Creates a Fraction Number having Numerator and Denumerator.</summary>
		///<param name="num">Numerator</param>
		///<param name="den">Denumerator</param>
   		public Fraction(uint num, uint den) 
   		{
   			this.num = Convert.ToInt32(num);
   			this.den = Convert.ToInt32(den);
   		}
		
		///<summary>Creates a Fraction Number having only Numerator and assuming Denumerator=1.</summary>
		///<param name="num">Numerator</param>
   		public Fraction(int num) 
   		{
      		this.num = num;
   		}

        /// <summary>
        /// Creates a Fraction number from a double, using the specified precision.  
        /// E.g., 
        ///    Fraction(1.23456, 2) will yield 123/100 (= 1.23)
        ///    Fraction(1.23456, 4) will yield 12346/10000 (=1.2346)
        /// </summary>
        /// <param name="d">The double</param>
        /// <param name="prec">The number of decimal places </param>
        public Fraction(double d, int prec)
        {
            this.den = (int) Math.Pow(10, prec);
            this.num = (int)Math.Round(d * den);
        }
   		
		///<summary>Used to display Fraction numbers like 12/17.</summary>
		public override string ToString()
		{
			if (den==1) return String.Format(CultureInfo.InvariantCulture, "{0}", num);
			//if ((den % 10) == 0 ) return String.Format("{0}", num / den);
            return String.Format(CultureInfo.InvariantCulture, "{0}/{1}", num, den);
		}
		
		///<summary>The Numerator</summary>
		public int Numerator 
		{
			get {return num;}
			set {num = value;}
		}

		///<summary>The Denumerator</summary>
		public int Denumerator 
		{
			get {return den;}
			set {den = value;}
		}
		
   		///<summary>Overloades operator + </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1013:OverloadOperatorEqualsOnOverloadingAddAndSubtract")]
        public static Fraction operator +(Fraction a, Fraction b) 
   		{
            if (a == null)
                throw new ArgumentNullException("a");
            if (b == null)
                throw new ArgumentNullException("b");

      		return new Fraction(a.num * b.den + b.num * a.den, a.den * b.den);
   		}

   		///<summary>Overloades operator * </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates")]
        public static Fraction operator *(Fraction a, Fraction b) 
   		{
            if (a == null)
                throw new ArgumentNullException("a");
            if (b == null)
                throw new ArgumentNullException("b");

            return new Fraction(a.num * b.num, a.den * b.den);
   		}

   		///<summary>Retrives double value of a Frction number. Enables casting to double.</summary>
   		public static implicit operator double(Fraction f) 
   		{
            if (f == null)
                return 0.0;

            return (double)f.num / f.den;
   		}

        public static implicit operator FractionStruct(Fraction f)
        {
            if (f == null)
                f = new Fraction(0, 1);

            FractionStruct fs;
            fs.num = f.Numerator;
            fs.den = f.Denumerator;
            return fs;
        }
	}
}
