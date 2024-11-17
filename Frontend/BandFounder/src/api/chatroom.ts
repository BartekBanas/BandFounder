import Cookies from "universal-cookie";
import {API_URL} from "../config";
import {getUserByName} from "../components/common/frequentlyUsed";
import {ChatRoom} from "../types/ChatRoom";
import {ChatRoomCreateDto} from "../types/ChatroomCreateDto";
import {mantineErrorNotification} from "../components/common/mantineNotification";
import {authorizedHeaders} from "../hooks/authentication";

export async function getMyChatrooms(): Promise<ChatRoom[]> {
    try {
        const jwt = new Cookies().get('auth_token');
        const response = await fetch(`${API_URL}/chatrooms`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${jwt}`
            }
        });

        let chatrooms: ChatRoom[] = await response.json();
        return chatrooms;
    } catch (e) {
        mantineErrorNotification('Failed to fetch conversations');
        throw e;
    }
}

export const createDirectChatroom = async (userName: string): Promise<any> => {
    try {
        const user = await getUserByName(userName);
        if (!user) {
            throw new Error('User not found');
        }

        const chatRoom: ChatRoomCreateDto = {
            chatRoomType: 'Direct',
            invitedAccountId: user.id
        };

        const response = await fetch(`${API_URL}/chatrooms`, {
            method: 'POST',
            headers: authorizedHeaders(),
            body: JSON.stringify(chatRoom)
        });

        if (!response.ok) {
            const errorData = await response.text();
            throw {status: response.status, message: errorData};
        }

        return await response.json();
    } catch (e) {
        console.error('Error creating chatroom:', e);
        throw e;
    }
};

export const leaveChatroom = async (chatRoomId: string): Promise<any> => {
    try {
        const jwt = new Cookies().get('auth_token');
        const response = await fetch(`${API_URL}/chatrooms/${chatRoomId}/leave`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${jwt}`
            }
        });
        return await response.text();
    } catch (e) {
        mantineErrorNotification('Failed to leave chatroom');
        console.error('Error leaving chatroom:', e);
    }
}