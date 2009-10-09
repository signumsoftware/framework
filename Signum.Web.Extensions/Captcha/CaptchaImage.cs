/** 
 * Copyright (C) 2007-2008 Nicholas Berardi, Managed Fusion, LLC (nick@managedfusion.com)
 * 
 * <author>Nicholas Berardi</author>
 * <author_email>nick@managedfusion.com</author_email>
 * <company>Managed Fusion, LLC</company>
 * <product>Url Rewriter and Reverse Proxy</product>
 * <license>Microsoft Public License (Ms-PL)</license>
 * <agreement>
 * This software, as defined above in <product />, is copyrighted by the <author /> and the <company /> 
 * and is licensed for use under <license />, all defined above.
 * 
 * This copyright notice may not be removed and if this <product /> or any parts of it are used any other
 * packaged software, attribution needs to be given to the author, <author />.  This can be in the form of a textual
 * message at program startup or in documentation (online or textual) provided with the packaged software.
 * </agreement>
 * <product_url>http://www.managedfusion.com/products/url-rewriter/</product_url>
 * <license_url>http://www.managedfusion.com/products/url-rewriter/license.aspx</license_url>
 */

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Web;
using System.Text;

namespace Signum.Web.Captcha
{
	/// <summary>
	/// Amount of random font warping to apply to rendered text
	/// </summary>
    public enum FontWarpFactor
	{
		None,
		Low,
		Medium,
		High,
		Extreme
	}

	/// <summary>
	/// Amount of background noise to add to rendered image
	/// </summary>
    public enum BackgroundNoiseLevel
	{
		None,
		Low,
		Medium,
		High,
		Extreme
	}

	/// <summary>
	/// Amount of curved line noise to add to rendered image
	/// </summary>
    public enum LineNoiseLevel
	{
		None,
		Low,
		Medium,
		High,
		Extreme
	}

	/// <summary>
	/// CAPTCHA Image
	/// </summary>
	/// <seealso href="http://www.codinghorror.com">Original By Jeff Atwood</seealso>
	internal class CaptchaImage
	{
		#region Static

		/// <summary>
		/// Gets the cached captcha.
		/// </summary>
		/// <param name="guid">The GUID.</param>
		/// <returns></returns>
        internal static CaptchaImage GetCachedCaptcha(string guid)
		{
			if (String.IsNullOrEmpty(guid))
				return null;

			return (CaptchaImage)HttpRuntime.Cache.Get(guid);
		}

		/// <summary>
		/// 
		/// </summary>
        internal static string[] RandomFontFamily = { "arial" };//{ "arial", "arial black", "comic sans ms", "courier new", "estrangelo edessa", "franklin gothic medium", "georgia", "lucida console", "lucida sans unicode", "mangal", "microsoft sans serif", "palatino linotype", "sylfaen", "tahoma", "times new roman", "trebuchet ms", "verdana" };

		/// <summary>
		/// 
		/// </summary>
        internal static Color[] RandomColor = { Color.Black };//Color.Red, Color.Green, Color.Blue, Color.Black, Color.Purple, Color.Orange };

		/// <summary>
		/// Gets or sets a string of available text characters for the generator to use.
		/// </summary>
		/// <value>The text chars.</value>
        internal static string TextChars { get; set; }

		/// <summary>
		/// Gets or sets the length of the text.
		/// </summary>
		/// <value>The length of the text.</value>
        internal static int? TextLength { get; set; }

		/// <summary>
		/// Gets and sets amount of random warping to apply to the <see cref="CaptchaImage"/> instance.
		/// </summary>
		/// <value>The font warp.</value>
        internal static FontWarpFactor? FontWarp { get; set; }

		/// <summary>
		/// Gets and sets amount of background noise to apply to the <see cref="CaptchaImage"/> instance.
		/// </summary>
		/// <value>The background noise.</value>
        internal static BackgroundNoiseLevel? BackgroundNoise { get; set; }

		/// <summary>
		/// Gets or sets amount of line noise to apply to the <see cref="CaptchaImage"/> instance.
		/// </summary>
		/// <value>The line noise.</value>
        internal static LineNoiseLevel? LineNoise { get; set; }

		/// <summary>
		/// Gets or sets the cache time out.
		/// </summary>
		/// <value>The cache time out.</value>
        internal static double? CacheTimeOut { get; set; }

		#endregion

		private int _height;
		private int _width;
		private Random _rand;

		#region Public Properties

		/// <summary>
		/// Returns a GUID that uniquely identifies this Captcha
		/// </summary>
		/// <value>The unique id.</value>
        internal string UniqueId { get; private set; }

		/// <summary>
		/// Returns the date and time this image was last rendered
		/// </summary>
		/// <value>The rendered at.</value>
        internal DateTime RenderedAt { get; private set; }

		/// <summary>
		/// Gets the randomly generated Captcha text.
		/// </summary>
		/// <value>The text.</value>
        internal string Text { get; private set; }

		/// <summary>
		/// Width of Captcha image to generate, in pixels
		/// </summary>
		/// <value>The width.</value>
        internal int Width
		{
			get { return _width; }
			set
			{
				if ((value <= 60))
					throw new ArgumentOutOfRangeException("width", value, "width must be greater than 60.");

				_width = value;
			}
		}

		/// <summary>
		/// Height of Captcha image to generate, in pixels
		/// </summary>
		/// <value>The height.</value>
        internal int Height
		{
			get { return _height; }
			set
			{
				if (value <= 30)
					throw new ArgumentOutOfRangeException("height", value, "height must be greater than 30.");

				_height = value;
			}
		}

		#endregion

		/// <summary>
		/// Initializes the <see cref="CaptchaImage"/> class.
		/// </summary>
		static CaptchaImage()
		{
			FontWarp = FontWarp ?? FontWarpFactor.Medium;
			BackgroundNoise = BackgroundNoise ?? BackgroundNoiseLevel.None;
			LineNoise = LineNoise ?? LineNoiseLevel.Low;
            TextLength = TextLength ?? 5;
			TextChars = TextChars ?? "ACDEFGHJKLNPQRTUVXYZ2346789";
            CacheTimeOut = CacheTimeOut ?? 60 * 15; //15 minutos
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CaptchaImage"/> class.
		/// </summary>
        internal CaptchaImage()
		{
			_rand = new Random();
			Width = 180;
			Height = 50;
			Text = GenerateRandomText();
			RenderedAt = DateTime.Now;
			UniqueId = Guid.NewGuid().ToString("N");
		}

		/// <summary>
		/// Forces a new Captcha image to be generated using current property value settings.
		/// </summary>
		/// <returns></returns>
        internal Bitmap RenderImage()
		{
			return GenerateImagePrivate();
		}

		/// <summary>
		/// Returns a random font family from the font whitelist
		/// </summary>
		/// <returns></returns>
		private string GetRandomFontFamily()
		{
			return RandomFontFamily[_rand.Next(0, RandomFontFamily.Length)];
		}

		/// <summary>
		/// generate random text for the CAPTCHA
		/// </summary>
		/// <returns></returns>
		private string GenerateRandomText()
		{
			StringBuilder sb = new StringBuilder(TextLength.Value);
			int maxLength = TextChars.Length;
			for (int n = 0; n <= TextLength - 1; n++)
				sb.Append(TextChars.Substring(_rand.Next(maxLength), 1));

			return sb.ToString();
		}

		/// <summary>
		/// Returns a random point within the specified x and y ranges
		/// </summary>
		/// <param name="xmin">The xmin.</param>
		/// <param name="xmax">The xmax.</param>
		/// <param name="ymin">The ymin.</param>
		/// <param name="ymax">The ymax.</param>
		/// <returns></returns>
		private PointF RandomPoint(int xmin, int xmax, int ymin, int ymax)
		{
			return new PointF(_rand.Next(xmin, xmax), _rand.Next(ymin, ymax));
		}

		/// <summary>
		/// Randoms the color.
		/// </summary>
		/// <returns></returns>
		private Color GetRandomColor()
		{
			return RandomColor[_rand.Next(0, RandomColor.Length)];
		}

		/// <summary>
		/// Returns a random point within the specified rectangle
		/// </summary>
		/// <param name="rect">The rect.</param>
		/// <returns></returns>
		private PointF RandomPoint(Rectangle rect)
		{
			return RandomPoint(rect.Left, rect.Width, rect.Top, rect.Bottom);
		}

		/// <summary>
		/// Returns a GraphicsPath containing the specified string and font
		/// </summary>
		/// <param name="s">The s.</param>
		/// <param name="f">The f.</param>
		/// <param name="r">The r.</param>
		/// <returns></returns>
		private GraphicsPath TextPath(string s, Font f, Rectangle r)
		{
			StringFormat sf = new StringFormat();
			sf.Alignment = StringAlignment.Near;
			sf.LineAlignment = StringAlignment.Near;
			GraphicsPath gp = new GraphicsPath();
			gp.AddString(s, f.FontFamily, (int)f.Style, f.Size, r, sf);
			return gp;
		}

		/// <summary>
		/// Returns the CAPTCHA font in an appropriate size
		/// </summary>
		/// <returns></returns>
		private Font GetFont()
		{
			float fsize;
			string fname = GetRandomFontFamily();

			switch (FontWarp)
			{
				case FontWarpFactor.None:
					goto default;
				case FontWarpFactor.Low:
					fsize = Convert.ToInt32(_height * 0.8);
					break;
				case FontWarpFactor.Medium:
					fsize = Convert.ToInt32(_height * 0.85);
					break;
				case FontWarpFactor.High:
					fsize = Convert.ToInt32(_height * 0.9);
					break;
				case FontWarpFactor.Extreme:
					fsize = Convert.ToInt32(_height * 0.95);
					break;
				default:
					fsize = Convert.ToInt32(_height * 0.7);
					break;
			}
			return new Font(fname, fsize, FontStyle.Bold);
		}

		/// <summary>
		/// Renders the CAPTCHA image
		/// </summary>
		/// <returns></returns>
		private Bitmap GenerateImagePrivate()
		{
			Bitmap bmp = new Bitmap(_width, _height, PixelFormat.Format24bppRgb);

			using (Graphics gr = Graphics.FromImage(bmp))
			{
				gr.SmoothingMode = SmoothingMode.AntiAlias;
				gr.Clear(Color.White);

				int charOffset = 0;
				double charWidth = _width / TextLength.Value;
				Rectangle rectChar;

				foreach (char c in Text)
				{
					// establish font and draw area
					using (Font fnt = GetFont())
					{
						using (Brush fontBrush = new SolidBrush(GetRandomColor()))
						{
							rectChar = new Rectangle(Convert.ToInt32(charOffset * charWidth), 0, Convert.ToInt32(charWidth), _height);

							// warp the character
							GraphicsPath gp = TextPath(c.ToString(), fnt, rectChar);
							WarpText(gp, rectChar);

							// draw the character
							gr.FillPath(fontBrush, gp);

							charOffset += 1;
						}
					}
				}

				Rectangle rect = new Rectangle(new Point(0, 0), bmp.Size);
				AddNoise(gr, rect);
				AddLine(gr, rect);
			}

			return bmp;
		}

		/// <summary>
		/// Warp the provided text GraphicsPath by a variable amount
		/// </summary>
		/// <param name="textPath">The text path.</param>
		/// <param name="rect">The rect.</param>
		private void WarpText(GraphicsPath textPath, Rectangle rect)
		{
			float WarpDivisor;
			float RangeModifier;

			switch (FontWarp)
			{
				case FontWarpFactor.None:
					goto default;
				case FontWarpFactor.Low:
					WarpDivisor = 6F;
					RangeModifier = 1F;
					break;
				case FontWarpFactor.Medium:
					WarpDivisor = 5F;
					RangeModifier = 1.3F;
					break;
				case FontWarpFactor.High:
					WarpDivisor = 4.5F;
					RangeModifier = 1.4F;
					break;
				case FontWarpFactor.Extreme:
					WarpDivisor = 4F;
					RangeModifier = 1.5F;
					break;
				default:
					return;
			}

			RectangleF rectF;
			rectF = new RectangleF(Convert.ToSingle(rect.Left), 0, Convert.ToSingle(rect.Width), rect.Height);

			int hrange = Convert.ToInt32(rect.Height / WarpDivisor);
			int wrange = Convert.ToInt32(rect.Width / WarpDivisor);
			int left = rect.Left - Convert.ToInt32(wrange * RangeModifier);
			int top = rect.Top - Convert.ToInt32(hrange * RangeModifier);
			int width = rect.Left + rect.Width + Convert.ToInt32(wrange * RangeModifier);
			int height = rect.Top + rect.Height + Convert.ToInt32(hrange * RangeModifier);

			if (left < 0)
				left = 0;
			if (top < 0)
				top = 0;
			if (width > this.Width)
				width = this.Width;
			if (height > this.Height)
				height = this.Height;

			PointF leftTop = RandomPoint(left, left + wrange, top, top + hrange);
			PointF rightTop = RandomPoint(width - wrange, width, top, top + hrange);
			PointF leftBottom = RandomPoint(left, left + wrange, height - hrange, height);
			PointF rightBottom = RandomPoint(width - wrange, width, height - hrange, height);

			PointF[] points = new PointF[] { leftTop, rightTop, leftBottom, rightBottom };
			Matrix m = new Matrix();
			m.Translate(0, 0);
			textPath.Warp(points, rectF, m, WarpMode.Perspective, 0);
		}


		/// <summary>
		/// Add a variable level of graphic noise to the image
		/// </summary>
		/// <param name="graphics1">The graphics1.</param>
		/// <param name="rect">The rect.</param>
		private void AddNoise(Graphics g, Rectangle rect)
		{
			int density;
			int size;

			switch (BackgroundNoise)
			{
				case BackgroundNoiseLevel.None:
					goto default;
				case BackgroundNoiseLevel.Low:
					density = 30;
					size = 40;
					break;
				case BackgroundNoiseLevel.Medium:
					density = 18;
					size = 40;
					break;
				case BackgroundNoiseLevel.High:
					density = 16;
					size = 39;
					break;
				case BackgroundNoiseLevel.Extreme:
					density = 12;
					size = 38;
					break;
				default:
					return;
			}

			SolidBrush br = new SolidBrush(GetRandomColor());
			int max = Convert.ToInt32(Math.Max(rect.Width, rect.Height) / size);

			for (int i = 0; i <= Convert.ToInt32((rect.Width * rect.Height) / density); i++)
				g.FillEllipse(br, _rand.Next(rect.Width), _rand.Next(rect.Height), _rand.Next(max), _rand.Next(max));

			br.Dispose();
		}

		/// <summary>
		/// Add variable level of curved lines to the image
		/// </summary>
		/// <param name="graphics1">The graphics1.</param>
		/// <param name="rect">The rect.</param>
		private void AddLine(Graphics g, Rectangle rect)
		{
			int length;
			float width;
			int linecount;

			switch (LineNoise)
			{
				case LineNoiseLevel.None:
					goto default;
				case LineNoiseLevel.Low:
					length = 4;
					width = Convert.ToSingle(_height / 31.25);
					linecount = 1;
					break;
				case LineNoiseLevel.Medium:
					length = 5;
					width = Convert.ToSingle(_height / 27.7777);
					linecount = 1;
					break;
				case LineNoiseLevel.High:
					length = 3;
					width = Convert.ToSingle(_height / 25);
					linecount = 2;
					break;
				case LineNoiseLevel.Extreme:
					length = 3;
					width = Convert.ToSingle(_height / 22.7272);
					linecount = 3;
					break;
				default:
					return;
			}

			PointF[] pf = new PointF[length + 1];
			using (Pen p = new Pen(GetRandomColor(), width))
			{
				for (int l = 1; l <= linecount; l++)
				{
					for (int i = 0; i <= length; i++)
						pf[i] = RandomPoint(rect);

					g.DrawCurve(p, pf, 1.75F);
				}
			}
		}
	}
}