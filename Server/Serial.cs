#region Header
// RpiUO - Serial.cs
// Last Edit: 2015/12/28
// Look for Rpi comment
// **********
#endregion

#region References
using System;
#endregion

namespace Server
{
	public struct Serial : IComparable, IComparable<Serial>
	{
        //Rpi - Refactored using the naming convention

        private readonly int serial_pr;

		private static Serial lastMobile_ps = Zero_sr;
		private static Serial lastItem_ps = 0x40000000;

		public static Serial LastMobile_s { get { return lastMobile_ps; } }
		public static Serial LastItem_s { get { return lastItem_ps; } }

		public static readonly Serial MinusOne_sr = new Serial(-1);
		public static readonly Serial Zero_sr = new Serial(0);

		public static Serial NewMobile
		{
			get
			{
				while (World.FindMobile(lastMobile_ps = (lastMobile_ps + 1)) != null)
				{
					;
				}

				return lastMobile_ps;
			}
		}

		public static Serial NewItem
		{
			get
			{
				while (World.FindItem(lastItem_ps = (lastItem_ps + 1)) != null)
				{
					;
				}

				return lastItem_ps;
			}
		}

		private Serial(int a_serial)
		{
			serial_pr = a_serial;
		}

		public int Value { get { return serial_pr; } }

		public bool IsMobile { get { return (serial_pr > 0 && serial_pr < 0x40000000); } }

		public bool IsItem { get { return (serial_pr >= 0x40000000 && serial_pr <= 0x7FFFFFFF); } }

		public bool IsValid { get { return (serial_pr > 0); } }

		public override int GetHashCode()
		{
			return serial_pr;
		}

		public int CompareTo(Serial other_serial)
		{
			return serial_pr.CompareTo(other_serial.serial_pr);
		}

		public int CompareTo(object other_object)
		{
			if (other_object is Serial)
			{
				return CompareTo((Serial)other_object);
			}
			else if (other_object == null)
			{
				return -1;
			}

			throw new ArgumentException();
		}

		public override bool Equals(object an_object)
		{
			if (an_object == null || !(an_object is Serial))
			{
				return false;
			}

			return ((Serial)an_object).serial_pr == serial_pr;
		}

		public static bool operator ==(Serial left_serial, Serial right_serial)
		{
			return left_serial.serial_pr == right_serial.serial_pr;
		}

		public static bool operator !=(Serial left_serial, Serial right_serial)
		{
			return left_serial.serial_pr != right_serial.serial_pr;
		}

		public static bool operator >(Serial left_serial, Serial right_serial)
		{
			return left_serial.serial_pr > right_serial.serial_pr;
		}

		public static bool operator <(Serial left_serial, Serial right_serial)
		{
			return left_serial.serial_pr < right_serial.serial_pr;
		}

		public static bool operator >=(Serial left_serial, Serial right_serial)
		{
			return left_serial.serial_pr >= right_serial.serial_pr;
		}

		public static bool operator <=(Serial left_serial, Serial right_serial)
		{
			return left_serial.serial_pr <= right_serial.serial_pr;
		}

		/*public static Serial operator ++ ( Serial l )
        {
        return new Serial( l + 1 );
        }*/

		public override string ToString()
		{
			return String.Format("0x{0:X8}", serial_pr.TostringLookup());
		}

		public static implicit operator int(Serial a_serial)
		{
			return a_serial.serial_pr;
		}

		public static implicit operator Serial(int a_serial)
		{
			return new Serial(a_serial);
		}
	}
}