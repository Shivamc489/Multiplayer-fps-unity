using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;

public class JoinGame : MonoBehaviour {

    List<GameObject> roomList = new List<GameObject>();

    [SerializeField]
    private Text status;

    [SerializeField]
    private GameObject roommListItemPrefab;

    [SerializeField]
    private Transform roomListParent;

    private NetworkManager networkManager;

    void Start()
    {
        networkManager = NetworkManager.singleton;
        if(networkManager.matchMaker==null)
        {
            networkManager.StartMatchMaker();
        }

        RefreshRoomList();
    }

    public void RefreshRoomList()
    {
        ClearRoomList();
        networkManager.matchMaker.ListMatches(0, 20, "", false, 0, 0, OnMatchList);
        status.text = "Loading...";
    }

    public void OnMatchList(bool success,string extendedInfo,List<MatchInfoSnapshot> matchList)
    {
        status.text = "";
        if(!success||matchList==null)
        {
            status.text = "No matches found...";
            return;
        }
        
        foreach (MatchInfoSnapshot match in matchList)
        {
            GameObject roomListItemGO = Instantiate(roommListItemPrefab);
            roomListItemGO.transform.SetParent(roomListParent);

            RoomListItem _roomListItem = roomListItemGO.GetComponent<RoomListItem>();
            if(_roomListItem!=null)
            {
                _roomListItem.Setup(match,JoinRoom);
            }

            

            roomList.Add(roomListItemGO);
        }

        if (roomList.Count==0)
        {
            status.text = "No Rooms Available";
        }
    }

    void ClearRoomList()
    {
        for (int i = 0; i < roomList.Count; i++)
        {
            Destroy(roomList[i]);
        }

        roomList.Clear();
    }

    public void JoinRoom(MatchInfoSnapshot _match)
    {
        networkManager.matchMaker.JoinMatch(_match.networkId, "","","",0,0, networkManager.OnMatchJoined);
        ClearRoomList();
        status.text = "JOINING...";
    }

}
