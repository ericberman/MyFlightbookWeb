using System;
using System.Drawing.Imaging;

namespace gma.Drawing.ImageInfo
{
    ///<summary>This class is designed to transform from PropertyItem received by calling 
    /// Image.GetPropertyItem() method. It transforms also the data that are stored in PropertyItem.Value byte array to ordinary .NET types.
    ///</summary>
    public class PropertyTag
	{
		readonly PropertyItem _prop;
	
		///<summary></summary>
		/// <param name="aPropertyItem"></param>
		public PropertyTag(PropertyItem aPropertyItem)
		{
			_prop=aPropertyItem;
		}
		
		///<summary>Specifies the data type of the values stored in the value of that PropertyItem object. Note that this are not ordinary .NET types.</summary>
        public PropertyTagType Type 
		{
			get {return (PropertyTagType)_prop.Type;}
			//set {_prop.Type = (short)value;}
		}
		
		///<summary>Id of a Property Tag</summary>
		public PropertyTagId Id
		{
			get {return (PropertyTagId)_prop.Id;}
			//set {_prop.Id =(int)value;}
		}
		
		///<summary>Display Name of a Property Tag</summary>
		public string TagName 
		{
			get {return getNameFromId(Id);}
		}
		
		///<summary>Transformed value of the PropertyItem.Value byte array.</summary>
		public Object Value
		{
			get{return getValue(_prop);}
		}
	
		private readonly static System.Text.ASCIIEncoding _encoding = new System.Text.ASCIIEncoding();
		///<summary>Transformes value of the PropertyItem.Value byte array to apropriate .NET Framework type.</summary>
		/// <param name="aPropertyItem"></param>
        public static Object getValue(PropertyItem aPropertyItem)
		{
			if (aPropertyItem==null) return null;
				switch ((PropertyTagType)aPropertyItem.Type) {
					
					case PropertyTagType.Byte: 
						if (aPropertyItem.Value.Length == 1)  return aPropertyItem.Value[0];
						return aPropertyItem.Value; 
					
					case PropertyTagType.ASCII:
						return _encoding.GetString( aPropertyItem.Value, 0, aPropertyItem.Len - 1);
					
					case PropertyTagType.Short:
							ushort[] _resultUShort = new ushort[aPropertyItem.Len / (16 / 8) ];
							for (int i=0; i<_resultUShort.Length ; i++)
								_resultUShort[i]=BitConverter.ToUInt16(aPropertyItem.Value,i*(16 / 8));
							if (_resultUShort.Length==1) return _resultUShort[0]; 
							return (_resultUShort);

					case PropertyTagType.Long :
							uint [] _resultUInt32 = new uint[aPropertyItem.Len / (32 / 8)];
							for (int i=0; i<_resultUInt32.Length; i++)
								_resultUInt32[i]=	BitConverter.ToUInt32(aPropertyItem.Value, i*(32 / 8));
							if (_resultUInt32.Length==1) return _resultUInt32[0]; 
							return _resultUInt32;
					
					case PropertyTagType.Rational :
							Fraction[] _resultRational = new Fraction[aPropertyItem.Len / (64 / 8)];
							uint uNominator;
							uint uDenominator;
							for (int i=0; i<_resultRational.Length; i++)
							{
								//uNominator=1;
								uNominator 	 = BitConverter.ToUInt32(aPropertyItem.Value, i*(64 / 8)  );
								uDenominator  = BitConverter.ToUInt32(aPropertyItem.Value, (i*(64 / 8)) + (32/8));

                                if (uNominator > 0x7FFFFFFF || uDenominator > 0x7FFFFFFF)
                                {
                                    // divide both numerator/denominator by 2 if they're too big.
                                    uNominator >>= 1;
                                    uDenominator >>= 1;
                                }
								_resultRational[i]= new Fraction(uNominator, uDenominator);
								
							}
							if (_resultRational.Length==1) return _resultRational[0]; 
							return _resultRational;
					
					case PropertyTagType.Undefined:
							if (aPropertyItem.Value.Length == 1)  return aPropertyItem.Value[0];
							return aPropertyItem.Value; 
					
					case PropertyTagType.SLONG:
							int [] _resultInt32 = new int[aPropertyItem.Len / (32 / 8)];
							for (int i=0; i<_resultInt32.Length; i++)
								_resultInt32[i]=	BitConverter.ToInt32(aPropertyItem.Value, i*(32/8));
							if (_resultInt32.Length==1) return _resultInt32[0]; 
							return _resultInt32;

					case PropertyTagType.SRational: 
							Fraction[] _resultSRational = new Fraction[aPropertyItem.Len / (64 / 8)];
							int sNominator;
							int sDenominator;
							for (int i=0; i<_resultSRational.Length; i++)
							{
								sNominator 	 = BitConverter.ToInt32(aPropertyItem.Value, i*(64/8));
								sDenominator  = BitConverter.ToInt32(aPropertyItem.Value, i*(64/8)+(32/8));
								_resultSRational[i]= new Fraction(sNominator, sDenominator);
							}
							if (_resultSRational.Length==1) return _resultSRational[0]; 							
							return _resultSRational;
					
					default:
						if (aPropertyItem.Value.Length == 1)  return aPropertyItem.Value[0];
						return aPropertyItem.Value; 
			}
		}
		
		///<summary>Returns the Display Name of a Property Item. The current imlementation will return a name of the Enumeration member.</summary>
		///<param name="aId">PropertyId to get description for.</param>
		private string getNameFromId(PropertyTagId aId) 
		{
			return aId.ToString();	
		}
	}
}
