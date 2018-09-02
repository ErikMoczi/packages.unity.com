using UnityEngine;

namespace ProGrids.Editor
{
	enum Axis {
		None = 0x0,
		X = 0x1,
		Y = 0x2,
		Z = 0x4,
		NegX = 0x8,
		NegY = 0x16,
		NegZ = 0x32
	}

	enum SnapUnit {
		Meter,
		Centimeter,
		Millimeter,
		Inch,
		Foot,
		Yard,
		Parsec
	}

	static class EnumExtension
	{
		/// <summary>
		/// Multiplies a Vector3 using the inverse value of an axis (eg, Axis.Y becomes Vector3(1, 0, 1) )
		/// </summary>
		/// <param name="v"></param>
		/// <param name="axis"></param>
		/// <returns></returns>
		public static Vector3 InverseAxisMask(Vector3 v, Axis axis)
		{
			switch(axis)
			{
				case Axis.X:
				case Axis.NegX:
					return Vector3.Scale(v, new Vector3(0f, 1f, 1f));

				case Axis.Y:
				case Axis.NegY:
					return Vector3.Scale(v, new Vector3(1f, 0f, 1f));

				case Axis.Z:
				case Axis.NegZ:
					return Vector3.Scale(v, new Vector3(1f, 1f, 0f));

				default:
					return v;
			}
		}

		public static Vector3 AxisMask(Vector3 v, Axis axis)
		{
			switch(axis)
			{
				case Axis.X:
				case Axis.NegX:
					return Vector3.Scale(v, new Vector3(1f, 0f, 0f));

				case Axis.Y:
				case Axis.NegY:
					return Vector3.Scale(v, new Vector3(0f, 1f, 0f));

				case Axis.Z:
				case Axis.NegZ:
					return Vector3.Scale(v, new Vector3(0f, 0f, 1f));

				default:
					return v;
			}
		}

		public static float SnapUnitValue(SnapUnit su)
		{
			switch(su)
			{
				case SnapUnit.Meter:
					return Defaults.Meter;
				case SnapUnit.Centimeter:
					return Defaults.Centimeter;
				case SnapUnit.Millimeter:
					return Defaults.Millimeter;
				case SnapUnit.Inch:
					return Defaults.Inch;
				case SnapUnit.Foot:
					return Defaults.Foot;
				case SnapUnit.Yard:
					return Defaults.Yard;
				case SnapUnit.Parsec:
					return Defaults.Parsec;
				default:
					return Defaults.Meter;
			}
		}
	}
}
