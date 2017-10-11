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
	//public string id;
	//public string what;
	public Vector3 position;
	//public string color;
};

[System.Serializable]
class Command
{
	public Object[] objects;
};

public class VizServer : MonoBehaviour {


	#if !UNITY_WEBGL
	Thread receiveThread; 
	UdpClient client; 
	#endif

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
		if (lastcommand != null && lastcommand.objects != null) {
			foreach (Object o in lastcommand.objects) {
				GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				sphere.transform.position = o.position;
				sphere.transform.SetParent (gameObject.transform);
			}
			lastcommand = null;

		}	
	}

	void OnMessage(string x) {
		Debug.LogError ("received " + x);	

		// https://docs.unity3d.com/ScriptReference/JsonUtility.FromJson.html
		// [System.Serializable]
		lastcommand = JsonUtility.FromJson<Command>(x);


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
				Debug.LogError ("Error: "+w.error);
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

