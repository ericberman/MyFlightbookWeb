using System;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Collections;
using System.Text;
using System.Globalization;
using MyFlightbook.Geography;

namespace gma.Drawing.ImageInfo
{
	///<summary>This Class retrives Image Properties using Image.GetPropertyItem() method 
	/// and gives access to some of them trough its public properties. Or to all of them
	/// trough its public property PropertyItems.
	///</summary>
	public class Info
	{
		///<summary>Wenn using this constructor the Image property must be set before accessing properties.</summary>
		public Info()
		{
		}
		
		///<summary>Creates Info Class to read properties of an Image given from a file.</summary>
		/// <param name="imageFileName">A string specifiing image file name on a file system.</param>
		public Info(string imageFileName)
		{
			_image= System.Drawing.Image.FromFile(imageFileName);
		}
		
		///<summary>Creates Info Class to read properties of a given Image object.</summary>
		/// <param name="anImage">An Image object to analise.</param>
		public Info(System.Drawing.Image anImage)
		{
			_image = anImage;
		}
		
		System.Drawing.Image _image;
		///<summary>Sets or returns the current Image object.</summary>
		public System.Drawing.Image Image 
		{
			set{_image = value;}
			get{return _image;}
		}
		
		///<summary>
		/// Type is PropertyTagTypeShort or PropertyTagTypeLong
		///Information specific to compressed data. When a compressed file is recorded, the valid width of the meaningful image must be recorded in this tag, whether or not there is padding data or a restart marker. This tag should not exist in an uncompressed file.
		/// </summary>
		public uint PixXDim
		{
			get
			{
				object tmpValue = PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.ExifPixXDim));
				if (tmpValue.GetType().ToString().Equals("System.UInt16")) return (uint)(ushort)tmpValue; 
				return (uint)tmpValue; 			
			}
		}
		///<summary>
		/// Type is PropertyTagTypeShort or PropertyTagTypeLong
		/// Information specific to compressed data. When a compressed file is recorded, the valid height of the meaningful image must be recorded in this tag whether or not there is padding data or a restart marker. This tag should not exist in an uncompressed file. Because data padding is unnecessary in the vertical direction, the number of lines recorded in this valid image height tag will be the same as that recorded in the SOF.
		/// </summary>
		public uint PixYDim
		{
			get
			{
				object tmpValue = PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.ExifPixYDim));
				if (tmpValue.GetType().ToString().Equals("System.UInt16")) return (uint)(ushort)tmpValue; 
				return (uint)tmpValue; 			
			}
		}
		
		///<summary>
		///Number of pixels per unit in the image width (x) direction. The unit is specified by PropertyTagResolutionUnit
		///</summary>
		public Fraction XResolution
		{
			get
			{
				return (Fraction) PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.XResolution));			
			}
		}
		
		///<summary>
		///Number of pixels per unit in the image height (y) direction. The unit is specified by PropertyTagResolutionUnit.
		///</summary>
		public Fraction YResolution
		{
			get 
			{ 
				return (Fraction) PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.YResolution ));
			}
		}
		
		///<summary>
		///Unit of measure for the horizontal resolution and the vertical resolution.
		///2 - inch 3 - centimeter
		///</summary>
		public ResolutionUnit ResolutionUnit
		{
			get
			{
				return (ResolutionUnit) PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.ResolutionUnit ));			
			}
		}
		
		///<summary>
		///Brightness value. The unit is the APEX value. Ordinarily it is given in the range of -99.99 to 99.99.
		///</summary>
		public Fraction Brightness
		{
			get
			{
				return (Fraction) PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.ExifBrightness));			
			}
		}
		
		///<summary>
		/// The manufacturer of the equipment used to record the image.
		///</summary>
		public string EquipMake
		{
			get
			{
				return (string) PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.EquipMake));			
			}
		}

        public string ImageDescription
        {
            get
            {
                string sz = "";
                if (_image.PropertyIdList != null)
                {
                    foreach (int idProp in _image.PropertyIdList)
                    {
                        if (idProp == (int)PropertyTagId.ImageDescription)
                        {
                            try
                            {
                                // we'll bypass getValue here to do utf8 decoding
                                PropertyItem pi = _image.GetPropertyItem((int)PropertyTagId.ImageDescription);
                                return Encoding.UTF8.GetString(pi.Value, 0, pi.Len - 1);
                            }
                            catch (ArgumentException) { }
                        }
                    }
                }
                return sz;
            }
            set
            {
                byte[] rgbSz = Encoding.UTF8.GetBytes(value);
                PropertyItem pitem = _image.PropertyItems[0]; // just get an existing property
                pitem.Type = (short)PropertyTagType.ASCII;
                pitem.Id = (int)PropertyTagId.ImageDescription;
                pitem.Len = rgbSz.Length + 1;

                byte[] rgb = new byte[pitem.Len];
                rgbSz.CopyTo(rgb, 0);
                rgb[pitem.Len - 1] = 0;
                pitem.Value = rgb;
                _image.SetPropertyItem(pitem);
            }
        }
		
		///<summary>
		/// The model name or model number of the equipment used to record the image.
		/// </summary>
		public string EquipModel
		{
			get
			{
				return (string) PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.EquipModel));			
			}
		}
		
		///<summary>
		///Copyright information.
		///</summary>
		public string Copyright
		{
			get
			{
				return (string) PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.Copyright));			
			}
		}
		

		///<summary>
		///Date and time the image was created.
		///</summary>		
		public string DateTime
		{
			get
			{
				return (string) PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.DateTime));			
			}
		}
		
		//The format is YYYY:MM:DD HH:MM:SS with time shown in 24-hour format and the date and time separated by one blank character (0x2000). The character string length is 20 bytes including the NULL terminator. When the field is empty, it is treated as unknown.
		private static DateTime ExifDTToDateTime(string exifDT)
		{
			exifDT = exifDT.Replace(' ', ':');
			string[] ymdhms = exifDT.Split(':');
            int years = int.Parse(ymdhms[0], CultureInfo.InvariantCulture);
            int months = int.Parse(ymdhms[1], CultureInfo.InvariantCulture);
            int days = int.Parse(ymdhms[2], CultureInfo.InvariantCulture);
            int hours = int.Parse(ymdhms[3], CultureInfo.InvariantCulture);
            int minutes = int.Parse(ymdhms[4], CultureInfo.InvariantCulture);
            int seconds = int.Parse(ymdhms[5], CultureInfo.InvariantCulture);
			return new DateTime(years, months, days, hours, minutes, seconds);
		}
		
		///<summary>
		///Date and time when the original image data was generated. For a DSC, the date and time when the picture was taken. 
		///</summary>
		public DateTime DTOrig
		{
			get 
			{
				string tmpStr = (string) PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.ExifDTOrig));			
				return ExifDTToDateTime(tmpStr);
			}
		}

		///<summary>
		///Date and time when the image was stored as digital data. If, for example, an image was captured by DSC and at the same time the file was recorded, then DateTimeOriginal and DateTimeDigitized will have the same contents.
		///</summary>
		public DateTime DTDigitized
		{
			get 
			{
				string tmpStr = (string) PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.ExifDTDigitized));			
				return ExifDTToDateTime(tmpStr);
			}
		}
		
		
		///<summary>
		///ISO speed and ISO latitude of the camera or input device as specified in ISO 12232.
		///</summary>		
		public ushort ISOSpeed
		{
			get
			{
				return (ushort) PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.ExifISOSpeed));			
			}
		}
		
		///<summary>
		///Image orientation viewed in terms of rows and columns.
		///</summary>				
		public Orientation Orientation
		{
			get
			{
				return (Orientation) PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.Orientation));			
			}
		}

		///<summary>
		///Actual focal length, in millimeters, of the lens. Conversion is not made to the focal length of a 35 millimeter film camera.
		///</summary>						
		public Fraction FocalLength
		{
			get
			{
				return (Fraction) PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.ExifFocalLength));			
			}
		}

		///<summary>
		///F number.
		///</summary>						
		public Fraction FNumber
		{
			get
			{
				return (Fraction) PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.ExifFNumber));			
			}
		}

		///<summary>
		///Class of the program used by the camera to set exposure when the picture is taken.
		///</summary>						
		public ExposureProg ExposureProg
		{
			get
			{
				return (ExposureProg) PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.ExifExposureProg));			
			}
		}

		///<summary>
		///Metering mode.
		///</summary>						
		public MeteringMode MeteringMode
		{
			get
			{
				return (MeteringMode) PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.ExifMeteringMode));			
			}
		}

        /// <summary>
        /// Does this have the geotag properties?
        /// </summary>
        public Boolean HasGeotag
        {
            get
            {
                bool fHasLatRef = false;
                bool fHasLat = false;
                bool fHasLongRef = false;
                bool fHasLong = false;

                if (_image.PropertyIdList != null)
                {
                    foreach (int idProp in _image.PropertyIdList)
                    {
                        switch (idProp)
                        {
                            case (int) PropertyTagId.GpsLatitudeRef:
                                fHasLatRef = true;
                                break;
                            case (int)PropertyTagId.GpsLatitude:
                                fHasLat = true;
                                break;
                            case (int)PropertyTagId.GpsLongitudeRef:
                                fHasLongRef = true;
                                break;
                            case (int)PropertyTagId.GpsLongitude:
                                fHasLong = true;
                                break;
                            default:
                                break;
                        }
                    }
                }

                return fHasLat && fHasLatRef && fHasLong && fHasLongRef;
            }
        }

        private void WriteLatLon(double d, bool fLatitude)
        {
            string sRef = fLatitude ? ((d < 0) ? "S" : "N") : ((d < 0) ? "W" : "E");

            DMSAngle dmsa = new DMSAngle(d);
            FractionStruct[] f = new FractionStruct[3];
            f[0] = (FractionStruct) new Fraction(dmsa.Degrees);
            f[1] = (FractionStruct) new Fraction(dmsa.Minutes);
            f[2] = (FractionStruct) new Fraction(dmsa.Seconds, 5);

            // Write reference
            PropertyItem pi = _image.PropertyItems[0];  // use an existing property
            pi.Type = (short)PropertyTagType.ASCII;
            pi.Id = (int)(fLatitude ? PropertyTagId.GpsLatitudeRef : PropertyTagId.GpsLongitudeRef);
            pi.Len = Encoding.ASCII.GetByteCount(sRef) + 1;
            pi.Value = new byte[pi.Len];
            byte[] rgbSz = Encoding.ASCII.GetBytes(sRef);
            rgbSz.CopyTo(pi.Value, 0);
            pi.Value[pi.Len - 1] = 0;
            _image.SetPropertyItem(pi);

            // Now write the value
            pi = _image.PropertyItems[0];
            pi.Type = (short)PropertyTagType.Rational;
            pi.Id = (int)(fLatitude ? PropertyTagId.GpsLatitude : PropertyTagId.GpsLongitude);
            pi.Len = Marshal.SizeOf(f[0]) * f.Length;
            pi.Value = new byte[pi.Len];
            IntPtr ptr = Marshal.AllocHGlobal(pi.Len);
            for (int i = 0; i < f.Length; i++)
                Marshal.StructureToPtr(f[i], new IntPtr(ptr.ToInt64() + i * Marshal.SizeOf(f[0])), true);
            Marshal.Copy(ptr, pi.Value, 0, pi.Len);
            Marshal.FreeHGlobal(ptr);
            _image.SetPropertyItem(pi);
        }

        /// <summary>
        /// Latitude
        /// </summary>
        public double Latitude
        {
            get {
                string sRef = "N";
                try
                {
                    sRef = (string)PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.GpsLatitudeRef));
                }
                catch (ArgumentException) { }
                Fraction[] f = (Fraction[])PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.GpsLatitude));
                double fDeg = (double)f[0];;
                double fMin = (double)f[1];
                double fSec = (double)f[2];

                return ((String.Compare(sRef, "S", StringComparison.OrdinalIgnoreCase) == 0 ? -1 : 1) * (fDeg + fMin / 60.0 + fSec / 3600.0));
            }
            set
            {
                WriteLatLon(value, true);
            }
        }

        /// <summary>
        /// Longitude
        /// </summary>
        public double Longitude
        {
            get
            {
                string sRef = "E";
                try
                {
                    sRef = (string)PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.GpsLongitudeRef));
                }
                catch (ArgumentException) { }
                Fraction[] f = (Fraction[])PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.GpsLongitude));
                double fDeg = (double)f[0]; ;
                double fMin = (double)f[1];
                double fSec = (double)f[2];

                return ((String.Compare(sRef, "W", StringComparison.CurrentCultureIgnoreCase) == 0 ? -1 : 1) * (fDeg + fMin / 60.0 + fSec / 3600.0));
            }
            set
            {
                WriteLatLon(value, false);
            }
        }
		
		private Hashtable _propertyItems;
		///<summary>
		/// Returns a Hashtable of all available Properties of a gieven Image. Keys of this Hashtable are
		/// Display names of the Property Tags and values are transformed (typed) data.
		///</summary>
		/// <example>
		/// <code>
		/// if (openFileDialog.ShowDialog()==DialogResult.OK)
		///	{
		///		Info inf=new Info(Image.FromFile(openFileDialog.FileName));
		///		listView.Items.Clear();
		///		foreach (string propertyname in inf.PropertyItems.Keys)
		///		{
		///			ListViewItem item1 = new ListViewItem(propertyname,0);
		///		    item1.SubItems.Add((inf.PropertyItems[propertyname]).ToString());
		///			listView.Items.Add(item1);
		///		}
		///	}
		/// </code>
		///</example>
		public Hashtable PropertyItems
		{
			get 
			{
				if (_propertyItems==null)
				{
					_propertyItems= new Hashtable();
					foreach(int id in _image.PropertyIdList)
						_propertyItems[((PropertyTagId)id).ToString()]=PropertyTag.getValue(_image.GetPropertyItem(id));
						
				}
				return _propertyItems;
			}
		}
	}
}
