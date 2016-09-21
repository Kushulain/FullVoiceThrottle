using UnityEngine;
using System.Collections;

[System.Serializable]
public class CoolDownEvent {
	
	public float lastGoTime;
	public float cooldownTime;

	public CoolDownEvent(float cooldown)
	{
		lastGoTime = -cooldown;
		cooldownTime = cooldown;
	}

	public CoolDownEvent()
	{
		lastGoTime = -1f;
		cooldownTime = 1f;
	}

	public bool TryGo(float cd = -1f)
	{
		if (Time.time > (lastGoTime+cooldownTime))
		{
			if (cd != -1f)
				cooldownTime = cd;

			lastGoTime = Time.time;
			return true;
		}
		return false;
	}

	public bool Go(float cd = -1f)
	{
		if (Time.time > (lastGoTime+cooldownTime))
		{
			if (cd != -1f)
				cooldownTime = cd;

			lastGoTime = Time.time;
			return true;
		}

		if (cd != -1f)
			cooldownTime = cd;

		lastGoTime = Time.time;
		return false;
	}

	public bool Available()
	{
		if (Time.time > (lastGoTime+cooldownTime))
			return true;
		return false;
	}

	public bool GoneForGreaterThan(float time)
	{
		if (Time.time > (lastGoTime+time))
			return true;
		return false;
	}

	public float Get01Progress()
	{
		return (Time.time-lastGoTime)/(cooldownTime <= 0f ? 1f : cooldownTime);
	}

	public float GetTimeLeft()
	{
		return (lastGoTime+cooldownTime)-Time.time;
	}
}

[System.Serializable]
public class LinearCurve {
	public Vector2[] curveDatas;

	LinearCurve() {}

	public LinearCurve(bool useY, params float[] values)
	{
		if (useY)
		{
			curveDatas = new Vector2[values.Length/2];

			for (int i=0; i<curveDatas.Length; i++)
			{
				curveDatas[i].x = values[i*2];
				curveDatas[i].y = values[i*2+1];
			}
		}
		else
		{
			curveDatas = new Vector2[values.Length];

			for (int i=0; i<curveDatas.Length; i++)
			{
				curveDatas[i].x = values[i];
				curveDatas[i].y = 0f;
			}
		}
	}

	public float Evaluate(float t)
	{
		if (curveDatas.Length == 0)
			return 1f;

		
		int right = -1;
		int left = -1;
		
		for (int i=0; i<curveDatas.Length; i++)
		{
			if (t < curveDatas[i].x && right == -1)
			{
				right = i;
			}
			if (t >= curveDatas[i].x)
			{
				left = i;
			}
		}
		
		if (right == -1) // t is OUT OF CURVE (RIGHT SIDE)
		{
			return curveDatas[left].y;
		}
		
		if (left == -1) // t is OUT OF CURVE (LEFT SIDE)
		{
			return curveDatas[right].y;
		}

		float weightRight = Mathf.InverseLerp(curveDatas[left].x,curveDatas[right].x,t);


		return (1f-weightRight)*curveDatas[left].y + weightRight*curveDatas[right].y;
	}

	public float GetWeightID(float t, int id)
	{
		if (id >= curveDatas.Length)
			return 0f;

		int right = -1;
		int left = -1;

		for (int i=0; i<curveDatas.Length; i++)
		{
			if (t < curveDatas[i].x && right == -1)
			{
				right = i;
			}
			if (t >= curveDatas[i].x)
			{
				left = i;
			}
		}

		if (right == -1) // t is OUT OF CURVE (RIGHT SIDE)
		{
			if (id == left)
				return 1f;
			else 
				return 0f;
		}

		if (left == -1) // t is OUT OF CURVE (LEFT SIDE)
		{
			if (id == right)
				return 1f;
			else 
				return 0f;
		}

		if (id == right)
			return Mathf.InverseLerp(curveDatas[left].x,curveDatas[right].x,t);
		if (id == left)
			return Mathf.InverseLerp(curveDatas[right].x,curveDatas[left].x,t);

		return 0f;
	}

}


[System.Serializable]
public class SinGenerator {

	public Vector2[] freqAndOffsets;
	public float globalOffset = 0f;
	
	SinGenerator() {}
	
	public SinGenerator(bool useY, params float[] values)
	{
		if (useY)
		{
			freqAndOffsets = new Vector2[values.Length/2];
			
			for (int i=0; i<freqAndOffsets.Length; i++)
			{
				freqAndOffsets[i].x = values[i*2];
				freqAndOffsets[i].y = values[i*2+1];
			}
		}
		else
		{
			freqAndOffsets = new Vector2[values.Length];
			
			for (int i=0; i<freqAndOffsets.Length; i++)
			{
				freqAndOffsets[i].x = values[i];
				freqAndOffsets[i].y = 0f;
			}
		}
	}

	public float Get()
	{
		float results = 1f;

		for (int i=0; i<freqAndOffsets.Length; i++)
		{
			results *= Mathf.Sin(Time.time*Mathf.PI*2f*freqAndOffsets[i].x + freqAndOffsets[i].y + globalOffset);
		}
		return results;
	}

	public float Get(float t)
	{
		float results = 1f;
		
		for (int i=0; i<freqAndOffsets.Length; i++)
		{
			results *= Mathf.Sin(t*Mathf.PI*2f*freqAndOffsets[i].x + freqAndOffsets[i].y + globalOffset);
		}
		return results;
	}

}
