import {API_URL} from "../config";
import {authorizedHeaders} from "../hooks/authentication";
import {mantineErrorNotification} from "../components/common/mantineNotification";
import {Message} from "../types/Message";

export async function getMessagesFromChatroom(chatroomId: string): Promise<Message[]> {
    try {
        const response = await fetch(`${API_URL}/chatrooms/${chatroomId}/messages`, {
            method: 'GET',
            headers: authorizedHeaders()
        });

        return await response.json() as Message[];
    } catch (e) {
        mantineErrorNotification('Failed to fetch messages');
        throw e;
    }
}

export async function sendMessage(chatroomId: string, message: string): Promise<void> {
    try {
        const response = await fetch(`${API_URL}/chatrooms/${chatroomId}/messages`, {
            method: 'POST',
            headers: authorizedHeaders(),
            body: JSON.stringify(message)
        });

        if (!response.ok) {
            throw new Error(await response.text());
        }

    } catch (e) {
        mantineErrorNotification('Failed to send message');
        throw e;
    }
}