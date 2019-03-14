using Burst.Compiler.IL.Tests.Helpers;
using Unity.Mathematics;

namespace Burst.Compiler.IL.Tests
{
    public partial class Vectors
    {
        public class Doubles
        {
            // ---------------------------------------------------
            // double4
            // ---------------------------------------------------

            [TestCompiler(DataRange.Standard)]
            public static double Double4Int(int a)
            {
                return ConvertToDouble(new double4(a));
            }

            [TestCompiler(DataRange.Standard)]
            public static double Double4Double3Double(double x)
            {
                return ConvertToDouble(new double4(new double3(x), 5.0f));
            }

            [TestCompiler(DataRange.Standard)]
            public static double Double4Double2Double2(double x)
            {
                return ConvertToDouble(new double4(new double2(x), new double2(5.0f)));
            }

            [TestCompiler(DataRange.Standard)]
            public static double Double44Doubles(double a)
            {
                return ConvertToDouble(new double4(1.0f, 2.0f, 3.0f + a, 4.0f));
            }

            [TestCompiler(DataRange.Standard)]
            public static double Double4Double(double a)
            {
                return ConvertToDouble(new double4(a));
            }

            [TestCompiler(DataRange.Standard)]
            public static double Double4Int4(ref int4 a)
            {
                return ConvertToDouble(new double4(a));
            }

       }
    }
}