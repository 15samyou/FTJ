using UnityEngine;
using System.Collections;

public class Net {
	public static int GetMyID(){
		return int.Parse(Network.player.ToString());
	}
};

public class CursorScript : MonoBehaviour {
	Color color_;
	GameObject held_;
	int id_;
	
	public int id() {
		return id_;
	}
	
	void SetColor(Vector3 color){
		color_ = new Color(color.x, color.y, color.z);
		Transform pointer = transform.FindChild("Pointer");
		Transform pointer_tint = pointer.FindChild("pointer_tint");
		Transform default_obj = pointer_tint.FindChild("default");
		default_obj.renderer.material.color = color_;	
	}
	
	void Start () {
		if(networkView.isMine){
			SetColor(new Vector3(Random.Range(0.0f,1.0f),
								 Random.Range(0.0f,1.0f),
								 Random.Range(0.0f,1.0f)));
		}
		id_ = Net.GetMyID();
		BoardScript.Instance().RegisterCursorObject(gameObject);
		Screen.showCursor = false;
	}
	
	void OnDestroy() {
		BoardScript.Instance().UnRegisterCursorObject(gameObject);
		Screen.showCursor = true;
	}
	
	
	
	[RPC]
	public void TellBoardAboutDiceClick(int die_id, int player_id){
		BoardScript.Instance().ClientClickedOnDie(die_id, player_id);
	}
	
	[RPC]
	public void TellBoardAboutMouseRelease(int player_id){
		BoardScript.Instance().ClientReleasedMouse(player_id);
	}
	
	void Update () {
		if(networkView.isMine){
			Vector3 pos = new Vector3();
			{
				Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
				RaycastHit raycast_hit = new RaycastHit();
				if(Physics.Raycast(ray, out raycast_hit, 100.0f, 1 << 8)){
					pos = raycast_hit.point - ray.direction;
				}
			}
			
			if(Input.GetMouseButtonDown(0)){
				Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
				RaycastHit raycast_hit = new RaycastHit();
				if(Physics.Raycast(ray, out raycast_hit)){
					if(raycast_hit.collider.gameObject.layer == 10){
						int id = raycast_hit.collider.gameObject.GetComponent<DiceScript>().id_;
						if(!Network.isServer){
							networkView.RPC("TellBoardAboutDiceClick",RPCMode.Server,id,id_);
						} else {
							TellBoardAboutDiceClick(id, id_);
						}
					}
				}
			}
			if(Input.GetMouseButtonUp(0)){
				if(!Network.isServer){
					networkView.RPC("TellBoardAboutMouseRelease",RPCMode.Server,id_);
				} else {
					TellBoardAboutMouseRelease(id_);
				}
			}		
			rigidbody.position = pos;
			transform.position = pos;
		}
	}
	
	void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
	{
		// Send data to server
		if (stream.isWriting)
		{
			Vector3 color = new Vector3(color_.r, color_.g, color_.b);
			stream.Serialize(ref color);
			int id = id_;
			stream.Serialize(ref id);
		}
		// Read data from remote client
		else
		{
			Vector3 color = new Vector3();
			stream.Serialize(ref color);
			SetColor(color);
			int id = id_;
			stream.Serialize(ref id);
			id_ = id;
		}
	}
}
