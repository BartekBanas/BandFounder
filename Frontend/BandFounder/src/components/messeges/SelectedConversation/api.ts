import {API_URL} from "../../../config";
import Cookies from "universal-cookie";

export const getSelectedConversation = async (id: string): Promise<any> => {
    try {
        const jwt = new Cookies().get('auth_token');
        const response = await fetch(`${API_URL}/chatrooms/${id}/messages`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${jwt}`
            }
        });
        return await response.json();
    } catch (e) {
        console.error('Error getting selected conversation:', e);
    }
}

export const getSelectedChatroom = async (id: string): Promise<any> => {
    try {
        const jwt = new Cookies().get('auth_token');
        const response = await fetch(`${API_URL}/chatrooms/${id}`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${jwt}`
            }
        });
        return await response.json();
    } catch (e) {
        console.error('Error getting selected chatroom:', e);
    }
}

export const sendMessage = async (chatroomId: string, message: string): Promise<any> => {
    try {
        const jwt = new Cookies().get('auth_token');
        const response = await fetch(`${API_URL}/chatrooms/${chatroomId}/messages?message=${message}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${jwt}`
            }
        });
        return await response.text();
    } catch (e) {
        console.error('Error sending message:', e);
    }
}