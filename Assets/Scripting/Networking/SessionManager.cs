using UnityEngine;

/// <summary>
/// The scene starts with a SessionManager, which allows use to choose whether this instance
/// will be client, server or both (=host).
/// </summary>
public class SessionManager : MonoBehaviour {
	MoveMaker controller;

	bool IsClient = false;
	bool IsServer = false;

	private void OnGUI() {
		GUILayout.BeginArea(new Rect(300, 10, 300, 300));
		if (!IsClient && !IsServer) {
			StartButtons();
		}
		GUILayout.EndArea();
	}

	void StartButtons() {
		if (GUILayout.Button("Client")) {
			StartClient();
		}
	}
	void StartClient() {
		Debug.Log($"Starting client: enabling controller");

		Client client = GetComponent<Client>();
		client.enabled = true;

		controller = FindFirstObjectByType<MoveMaker>();
		controller.enabled = true;

		IsClient = true;
	}	
}
