import {API_URL} from "../../config";
import {ChatroomDto} from "../../types/chatroomDto";
import {authorizedHeaders} from "../../hooks/authentication";
import {ChatRoomCreateDto} from "../../types/ChatroomCreateDto";

export async function contactProfileOwner(profileId: string): Promise<ChatroomDto | undefined> {
    try {
        const newChatroom: ChatRoomCreateDto = {
            chatRoomType: 'Direct',
            invitedAccountId: profileId
        }
        const response = await fetch(`${API_URL}/chatrooms`, {
            method: 'POST',
            headers: authorizedHeaders(),
            body: JSON.stringify(newChatroom)
        });

        if (!response.ok) {
            throw new Error('Failed to contact the listing owner');
        }

        const chatroom: ChatroomDto = await response.json();
        return chatroom;
    } catch (error) {
        console.error(error);
    }
}