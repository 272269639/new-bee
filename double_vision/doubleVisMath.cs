using System;
using System.Collections.Generic;
using System.Text;
/*************************************************************************
类：计算类
作用：世界坐标计算
*************************************************************************/
namespace TwoCamerasVision
{
    class doubleVisMath
    {
        public static double colculateX(float t, float x1, float x2)
        {

            return t * x1 / (x1 - x2);
        }
        public static double colculateY(float t, float x1, float x2,float y1)
        {
            return t * y1 / (x1 - x2);
        }
        public static double colculateZ(float t, float x1, float x2, float f) {
            return t * f / (x1 - x2);
        }
        public static double polarX(double z,double Angle,float temp) {
            return z * temp * (System.Math.Cos(Angle / 360 * 6.28));
        }
        public static double polarZ(double z, double Angle,float temp)
        {
            return z * temp* (System.Math.Sin(Angle / 360 * 6.28));
        }
        public static double polarY(double Y) {
            return Y;
        }
    }
}
