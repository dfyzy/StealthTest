using System;
using System.Collections.Generic;
using UnityEngine;

public static class StolenMathf
{
	//via https://github.com/FreyaHolmer/Mathfs/blob/master/Mathfs.cs
	public enum PolynomialType {
		Constant,
		Linear,
		Quadratic
	}

	public static PolynomialType GetPolynomialType( float a, float b, float c ) {
		if( Mathf.Abs( a ) < 0.00001f )
			return Mathf.Abs( b ) < 0.00001f ? PolynomialType.Constant : PolynomialType.Linear;
		return PolynomialType.Quadratic;
	}

	public static List<float> GetQuadraticRoots( float a, float b, float c ) { // ax² + bx + c
		List<float> roots = new List<float>();

		switch( GetPolynomialType( a, b, c ) ) {
			case PolynomialType.Constant:
				break; // either no roots or infinite roots if c == 0
			case PolynomialType.Linear:
				roots.Add( -c / b );
				break;
			case PolynomialType.Quadratic:
				float rootContent = b * b - 4 * a * c;
				if( Mathf.Abs( rootContent ) < 0.0001f ) {
					roots.Add( -b / ( 2 * a ) ); // two equivalent solutions at one point
				} else if( rootContent >= 0 ) {
					float root = Mathf.Sqrt( rootContent );
					roots.Add( ( -b + root ) / ( 2 * a ) ); // crosses at two points
					roots.Add( ( -b - root ) / ( 2 * a ) );
				} // else no roots

				break;
		}

		return roots;
	}
	//

	//via stack overflow
	public static float UnwindAngleDegrees(float angle)
	{
		angle %= 360f;
		if (angle < -180.0f)
			angle += 360f;
		else if (angle > 180.0f)
			angle -= 360f;

		return angle;
	}
	//

	//not actually stolen
	public static bool GetMinRootInRange(List<float> roots, float min, float max, out float result)
	{
		result = max + 1.0f;
		bool success = false;
		foreach (float root in roots)
		{
			if (root >= min && root <= max && root < result)
			{
				result = root;
				success = true;
			}
		}

		return success;
	}

	public static float Sinh(float f)
	{
		return (Mathf.Exp(f) - Mathf.Exp(-f))*0.5f;
	}

	public static float Cosh(float f)
	{
		return (Mathf.Exp(f) + Mathf.Exp(-f))*0.5f;
	}

	public static float Asinh(float f)
	{
		return Mathf.Log(f + Mathf.Sqrt(f*f + 1));
	}

	public static float Acosh(float f)
	{
		return Mathf.Log(f + Mathf.Sqrt(f*f - 1));
	}

	public static Vector3 MinSize(Vector3 v, float f)
	{
		float size = v.magnitude;
		return size > f ? v*(f/size) : v;
	}

	public static Quaternion FromAngleAxis(Vector3 angleAxis)
	{
		float size = angleAxis.magnitude;
		return size > 0.00001f ? Quaternion.AngleAxis(Mathf.Rad2Deg*size, angleAxis/size) : Quaternion.identity;
	}

	public static Vector3 ToAngleAxis(Quaternion quat)
	{
		float angle; Vector3 axis;
		quat.ToAngleAxis(out angle, out axis);

		angle = UnwindAngleDegrees(angle);
		return angle == 0.0f ? Vector3.zero : axis*(Mathf.Deg2Rad*angle);
	}
//

	//https://pomax.github.io/bezierinfo/
	public static class Bezier
	{
		public static float[] Basis3(float t)
		{
			float t2 = t*t;
			float mt = 1.0f - t;
			float mt2 = mt*mt;

			return new float[]{mt2*mt, 3.0f*mt2*t, 3.0f*mt*t2, t2*t};
		}

		public static Vector3 Calc3(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
		{
			float[] ts = Basis3(t);
			return p0*ts[0] + p1*ts[1] + p2*ts[2] + p3*ts[3];
		}
	};

}
