using System.Collections.Generic;
using UnityEngine;

public class RoomManager
{

    public class RoomData
    {
        public bool hostJoined = false;
        public bool guestJoined = false;
        public float expirationTime; // Time.time 기준으로 유효 시간
    }

    public static Dictionary<string, RoomData> ActiveRooms = new Dictionary<string, RoomData>();

    public static bool CreateRoom(string code, float durationSeconds = 180f)
    {
        if (ActiveRooms.ContainsKey(code)) return false;

        ActiveRooms[code] = new RoomData
        {
            hostJoined = true,
            expirationTime = Time.time + durationSeconds
        };

        return true;
    }

    public static bool JoinRoom(string code)
    {
        if (!ActiveRooms.ContainsKey(code)) return false;

        RoomData room = ActiveRooms[code];

        // 만료된 방이면 참가 불가
        if (Time.time > room.expirationTime)
        {
            ActiveRooms.Remove(code);
            return false;
        }

        room.guestJoined = true;
        return true;
    }

    public static bool IsRoomReady(string code)
    {
        if (!ActiveRooms.ContainsKey(code)) return false;
        RoomData room = ActiveRooms[code];
        return room.hostJoined && room.guestJoined;
    }


}
