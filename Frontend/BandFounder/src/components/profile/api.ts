// src/components/profile/api.ts
import Cookies from "universal-cookie";
import { API_URL } from "../../config";
import { Account } from "../../types/Account";
import {ChatroomDto} from "../../types/chatroomDto";
import {authorizedHeaders} from "../../hooks/authentication";
import {ChatRoomCreateDto} from "../../types/ChatroomCreateDto";

const MAX_GENRES_RETURNED = 10;

export const getGUID = async (username: string) => {
    try {
        const response = await fetch(`${API_URL}/accounts?username=${username}`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${new Cookies().get('auth_token')}`
            }
        });
        if (!response.ok) {
            throw new Error(await response.text());
        }
        const account: Account = await response.json();
        return account.id;
    } catch (error) {
        console.error('Error getting GUID:', error);
    }
};

export const getAccount = async (guid: string) => {
    try {
        const response = await fetch(`${API_URL}/accounts/${guid}`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${new Cookies().get('auth_token')}`
            }
        });
        if (!response.ok) {
            throw new Error(await response.text());
        }
        // console.log('response:', response);
        return await response.json();
    } catch (error) {
        console.error('Error getting account:', error);
    }
};

export const getTopArtists = async (guid: string) => {
    try{
        const response = await fetch(`${API_URL}/accounts/${guid}/artists/spotify/top`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${new Cookies().get('auth_token')}`
            }
        });
        if (!response.ok) {
            throw new Error(await response.text());
        }
        return await response.json();
    }
    catch(error){
        console.error('Error getting top artists:', error);
    }
}

export const getTopGenres = async (guid: string): Promise<string[]> => {
    try {
        const response = await fetch(`${API_URL}/accounts/${guid}/genres`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${new Cookies().get('auth_token')}`
            }
        });
        if (!response.ok) {
            throw new Error(await response.text());
        }
        const genres: string[] = await response.json();
        return genres.slice(0, MAX_GENRES_RETURNED);
    } catch (error) {
        console.error('Error getting top genres:', error);
        return [];
    }
}

export async function contactProfileOwner(profileId: string): Promise<ChatroomDto | undefined> {
    try {
        const newChatroom : ChatRoomCreateDto = {
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