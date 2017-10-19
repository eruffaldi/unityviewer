using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;
using System.IO;

#if UNITY_WEBGL
#else
	using System.Net;
	using System.Net.Sockets;
	using System.Threading;

	public class ThreadedAction
	{
		public ThreadedAction(Action action)
		{
			var thread = new Thread(() => {
				if(action != null)
					action();
				_isDone = true;
			});
			thread.Start();
		}

		public IEnumerator WaitForComplete()
		{
			while (!_isDone)
				yield return null;
		}

		private bool _isDone = false;
	}
#endif

[System.Serializable]
class Object
{
	public UInt64 timestamp;
	public UInt32 frame;
	public string printorder; // openpose
	public string id="x"; 
	public float radius = 0.01f;
	public Color color = new Color(1,0,0); // color
	public string type;
	//public string id;
	//public string what;
	public Vector3 []points;
	//public string color;
};

[System.Serializable]
class Command
{
	public Object[] pointsets;
};

public class VizServer : MonoBehaviour {


	#if !UNITY_WEBGL
	Thread receiveThread; 
	UdpClient client; 
	#endif

	Dictionary<string, List<GameObject> > entities = new Dictionary<string, List<GameObject>>();


	public int port = 8051;
	public string url = "ws://127.0.0.1:8000";

	// Use this for initialization
	void Start () {
		

		#if !UNITY_WEBGL
		receiveThread = new Thread(new ThreadStart(ReceiveDataUdp));
		receiveThread.IsBackground = true;
		receiveThread.Start(); 
		#endif

		StartCoroutine (ReceiveDataWSX());
	}

	Command lastcommand;

	// Update is called once per frame
	void Update () {
		// check for new set
		if (lastcommand != null && lastcommand.pointsets != null) {
			// for each pointset
			Debug.Log("processing pointsets " + lastcommand.pointsets.Length);
			foreach (Object o in lastcommand.pointsets) {
				Debug.Log ("processing pointsets " +  o.points.Length);
			
				// lookup for obejct id
				var q = GameObject.Find ("points_" + o.id);
				List<GameObject> li;
				if (q == null)
				{
					q = new GameObject ();
					q.name = "points_" + o.id;
					q.transform.SetParent (gameObject.transform);
					li = new List<GameObject> ();
					entities [o.id] = li;
					Debug.Log ("creating object points_"+ o.id);
				}
				else
				{
					// lookup list
					li = entities[o.id];
					Debug.Log ("reusing object id "+ o.id);
				}
				float radius = o.radius;
				Vector3 vs = new Vector3 (radius,radius, radius);
				bool done = false;
				IEnumerator<GameObject> e = li.GetEnumerator ();
				//foreach(GameObject x in li)
				//	x.renderer.GetComponent<Renderer> ().enabled = false;
				for(int i = 0; i < o.points.Length; i++)
				{
					GameObject sphere = null;
					if (!done && e.MoveNext ()) {
						sphere = e.Current;
					}
					if (sphere == null) {
						sphere = GameObject.CreatePrimitive (PrimitiveType.Sphere);
						sphere.transform.SetParent (q.transform);
						li.Add (sphere);							
						done = true;
					}
					else
						Debug.Log("reusing sphere ");
					sphere.GetComponent<Renderer> ().material.color = o.color;
					sphere.transform.localScale = vs;
					sphere.GetComponent<Renderer> ().enabled = true;

					if (Single.IsNaN (o.points [i].x) == false) {
						sphere.transform.position = o.points [i];
					}
				}
				// some object left ... hide it

				if(!done) // this is not working!
				{
					while (e.MoveNext ()) {
						e.Current.GetComponent<Renderer> ().enabled = false;
					}
				}
				if (o.printorder == "openpose") {
					// TODO multiple line renderer
					LineRenderer lr = gameObject.GetComponent<LineRenderer> ();
					lr.SetPositions (o.points);
					lr.positionCount = o.points.Length;
				}
					
			}
			lastcommand = null;

		}	
	}

	void OnMessage(string x) {
 		Debug.Log ("received " + x);	

		// https://docs.unity3d.com/ScriptReference/JsonUtility.FromJson.html
		// [System.Serializable]
		lastcommand = JsonUtility.FromJson<Command>(x);
		if (lastcommand == null || lastcommand.pointsets  == null) {
			lastcommand = new Command ();
			lastcommand.pointsets = new Object[1];
			lastcommand.pointsets [0] = JsonUtility.FromJson<Object> (x);
		}


	}

	private IEnumerator ReceiveDataWSX()
	{
		WebSocket w = new WebSocket(new Uri(url));
		yield return StartCoroutine(w.Connect());
		while (true)
		{
			byte [] reply = w.Recv (); // non blocking
			if (reply != null)
			{
				string s = Encoding.UTF8.GetString(reply, 0, 4);
				OnMessage(s);
			}
			if (w.error != null)
			{
				Debug.Log ("Error: "+w.error);
				break;
			}
			yield return 0;
		}
		w.Close();
	}
	/*
#if !UNITY_WEBGL
	private void ReceiveDataWSTH()
	{
		var extractAction = new ThreadedAction(ReceiveDataWSX);
		StartCoroutine(extractAction.WaitForComplete());
	}
#endif
*/
	#if !UNITY_WEBGL
	private  void ReceiveDataUdp() 
	{
		client = new UdpClient(port);
		while (true) 
		{
			try 
			{
				IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
				byte[] data = client.Receive(ref anyIP);
				string text = Encoding.UTF8.GetString(data);
				OnMessage(text);
			}
			catch (Exception err) 
			{
				print(err.ToString());
			}
		}
	}
	#endif

}

