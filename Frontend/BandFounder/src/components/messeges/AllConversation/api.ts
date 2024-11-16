import Cookies from "universal-cookie";
import { API_URL } from "../../../config";
import { getUserById, getUserByName } from "../../common/frequentlyUsed";
import { ChatRoom } from "../../../types/ChatRoom";
import { ChatRoomCreateDto } from "../../../types/ChatroomCreateDto";

export const getAllConversations = async (): Promise<ChatRoom[] | undefined> => {
    try {
        const jwt = new Cookies().get('auth_token');
        const response = await fetch(`${API_URL}/chatrooms`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${jwt}`
            }
        });
        let chatRooms: ChatRoom[] = await response.json();
        console.log(chatRooms)
        for(let i = 0; i < chatRooms.length; i++){
            if(chatRooms[i].membersIds.length === 2) {
                const user = await getUserById(chatRooms[i].membersIds[0] === new Cookies().get('user_id') ? chatRooms[i].membersIds[1] : chatRooms[i].membersIds[0]);
                chatRooms[i].name = chatRooms[i].name + " with " + user.name;
            }
        }
        console.log(chatRooms)
        return chatRooms;
    } catch (e) {
        console.error('Error getting conversations:', e);
    }
};

export const getAllUsers = async (): Promise<any> => {
    try {
        const jwt = new Cookies().get('auth_token');
        const response = await fetch(`${API_URL}/accounts`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${jwt}`
            }
        });
        return await response.json();
    } catch (e) {
        console.error('Error getting users:', e);
    }
};

export const createNewChatroom = async (userName: string): Promise<any> => {
    try {
        const jwt = new Cookies().get('auth_token');
        const user = await getUserByName(userName);
        if (!user) {
            throw new Error('User not found');
        }
        console.log(user);
        const chatRoom: ChatRoomCreateDto = {
            chatRoomType: 'Direct',
            invitedAccountId: user.id
        };
        console.log(chatRoom);
        const response = await fetch(`${API_URL}/chatrooms`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${jwt}`
            },
            body: JSON.stringify(chatRoom)
        });
        return await response.json();
    } catch (e) {
        console.error('Error creating chatroom:', e);
    }
};
