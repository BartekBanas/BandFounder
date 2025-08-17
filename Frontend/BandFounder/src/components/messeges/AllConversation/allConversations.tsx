import React, {FC, useState, useEffect} from "react";
import {ChatRoom, ChatRoomType} from "../../../types/ChatRoom";
import {getMyChatrooms, createDirectChatroom} from "../../../api/chatroom";
import {Autocomplete, CircularProgress, TextField} from "@mui/material";
import './styles.css'
import '../../../styles/customScrollbar.css'
import {getUserId} from "../../../hooks/authentication";
import {getAccounts, getUser} from "../../../api/account";
import {Account} from "../../../types/Account";
import {mantineErrorNotification} from "../../common/mantineNotification";
import {LeaveChatroomModal} from "./LeaveChatroomModal";
import ProfilePicture from "../../profile/ProfilePicture";
import defaultProfileImage from "../../../assets/defaultProfileImage.jpg";

interface AllConversationsProps {
    onSelectConversation: (id: string) => void;
}

function ChatroomAvatar({chatRoom, myId}: { chatRoom: ChatRoom, myId: string }) {
    switch (chatRoom.type) {
        case ChatRoomType.Direct: {
            const otherUserId = chatRoom.membersIds.find(id => id !== myId);
            return (
                <ProfilePicture
                    accountId={otherUserId}
                    isMyProfile={false}
                    size={40}
                />
            );
        }

        case ChatRoomType.General: // TODO 103
            return (
                <img src={defaultProfileImage} alt="Default Profile"/>
            );

        default:
            throw new Error(`Unsupported chatroom type '${chatRoom.type}'`);
    }
}

export const AllConversations: FC<AllConversationsProps> = ({onSelectConversation}) => {
    const [chatRooms, setChatRooms] = useState<ChatRoom[]>([]);
    const [otherUsers, setOtherUsers] = useState<Account[]>([]);
    const [refreshConversations, setRefreshConversations] = useState<boolean>(false);
    const myId = getUserId();

    useEffect(() => {
        fetchConversations();
        fetchOtherUsers();
    }, [refreshConversations]);

    const fetchConversations = async () => {
        const chatrooms = await getMyChatrooms();

        for (const chatroom of chatrooms) {
            if (chatroom.type === ChatRoomType.Direct) {
                const otherUserId = chatroom.membersIds.find((id) => id !== getUserId());
                if (otherUserId) {
                    const user = await getUser(otherUserId);
                    chatroom.name = user.name;
                    chatroom.membersIds = [otherUserId];
                } else {
                    chatroom.name = 'Unknown';
                }
            }
        }

        if (chatrooms) {
            setChatRooms(chatrooms);
        }
    };

    const fetchOtherUsers = async () => {
        const myId = getUserId();
        const accounts = await getAccounts();
        setOtherUsers(accounts.filter((account: Account) => account.id !== myId));
    };

    const checkIdOfChatroom = (userName: string): string => {
        let id: string = '';

        chatRooms.forEach((chatRoom) => {
            if (chatRoom.name.toLowerCase() === userName.toLowerCase()) {
                id = chatRoom.id;
            }
        });
        return id;
    }

    const handleCreateChatroom = async (userName: string | null) => {
        if (!userName) return;

        try {
            const userId = otherUsers.find((user) => user.name === userName)?.id;
            if (typeof userId === "string") {
                const newChatRoom = await createDirectChatroom(userId);
                window.location.href = `/messages/${newChatRoom.id}`;
                await fetchConversations();
            } else {
                throw new Error('User not found');
            }
        } catch (error: any) {
            if (error.status === 409) {
                const chatroomId = checkIdOfChatroom(userName);
                window.location.href = `/messages/${chatroomId}`;
            } else {
                mantineErrorNotification('Failed to create chatroom with ' + userName);
                console.error("Error creating chatroom:", error);
            }
        }
    };

    const handleSelectConversation = (id: string) => {
        window.location.href = `/messages/${id}`;
    };

    return (
        <div id="mainChatroomsList">
            <Autocomplete
                options={otherUsers.map((user) => user.name)}
                onChange={(event, newValue) => {
                    handleCreateChatroom(newValue);
                }}
                renderInput={(params) => <TextField {...params} label="Search Users" variant="outlined"/>}
                style={{margin: "20px", maxWidth: "100%"}}
            />
            <div style={{display: 'flex', justifyContent: 'center', alignItems: 'center'}}>
                {
                    chatRooms.length > 0 ? <h2>Open Conversations</h2> :
                        <CircularProgress size={30} sx={{marginBottom: '20px'}}/>
                }
            </div>
            <ul id={'openConversationsList'} className={'custom-scrollbar'}>
                {chatRooms.map((chatRoom) => (
                    <li className={'singleConversationShortcut'} key={chatRoom.id}
                        onClick={() => handleSelectConversation(chatRoom.id)}>
                        <ChatroomAvatar chatRoom={chatRoom} myId={myId}/>
                        <div>{chatRoom.name}</div>
                        <div onClick={(e) => e.stopPropagation()}>
                            <LeaveChatroomModal
                                chatroom={chatRoom}
                                setRefreshConversations={setRefreshConversations}
                            />
                        </div>
                    </li>
                ))}
            </ul>
        </div>
    );
};