import {FC, useState, useEffect} from "react";
import {ChatRoom} from "../../../types/ChatRoom";
import {getMyChatrooms, createDirectChatroom, leaveChatroom} from "../../../api/chatroom";
import {Autocomplete, CircularProgress, TextField} from "@mui/material";
import {useNavigate} from "react-router-dom";
import './styles.css'
import defaultProfileImage from '../../../assets/defaultProfileImage.jpg';
import './../../../assets/CustomScrollbar.css'
import {getUserId} from "../../../hooks/authentication";
import {getAccounts, getUser} from "../../../api/account";
import {Account} from "../../../types/Account";
import {mantineErrorNotification} from "../../common/mantineNotification";
import {LeaveChatroomModal} from "./LeaveChatroomModal";

interface AllConversationsProps {
    onSelectConversation: (id: string) => void;
}

export const AllConversations: FC<AllConversationsProps> = ({onSelectConversation}) => {
    const [chatRooms, setChatRooms] = useState<ChatRoom[]>([]);
    const [otherUsers, setOtherUsers] = useState<string[]>([]);
    const [selectedUser, setSelectedUser] = useState<string | null>(null);
    const [refreshConversations, setRefreshConversations] = useState<boolean>(false);
    const navigate = useNavigate();

    useEffect(() => {
        fetchConversations();
        fetchOtherUsers();
    }, [refreshConversations]);

    const fetchConversations = async () => {
        const chatrooms = await getMyChatrooms();

        for (const chatroom of chatrooms) {
            if (chatroom.type === 'Direct') {
                const otherUserId = chatroom.membersIds.find((id) => id !== getUserId());
                if (otherUserId) {
                    const user = await getUser(otherUserId);
                    chatroom.name = user.name;
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
        accounts.filter((account: Account) => account.id !== myId);
        setOtherUsers(accounts.map((user: any) => user.name));
    };

    const checkIdOfChatroom = (userName: string): string => {
        let id: string = '';

        chatRooms.forEach((chatRoom) => {
            if (chatRoom.name.toLowerCase() == userName.toLowerCase()) {
                id = chatRoom.id;
            }
        });
        return id;
    }

    const handleCreateChatroom = async (userName: string | null) => {
        if (!userName) return;

        try {
            const newChatRoom = await createDirectChatroom(userName);
            fetchConversations();
        } catch (error: any) {
            if (error.status === 409) {
                const chatroomId = await checkIdOfChatroom(userName);
                window.location.href = `/messages/${chatroomId}`;
            } else {
                mantineErrorNotification('Failed to create chatroom with ' + userName);
                console.error("Error creating chatroom:", error);
            }
        }
    };

    const handleLeaveChatroom = async (chatRoomId: string) => {
        try {
            console.log(`Leaving chatroom with id: ${chatRoomId}`);
            await leaveChatroom(chatRoomId);
            window.location.href = '/messages';
        } catch (error) {
            console.error("Error leaving chatroom:", error);
        }
    };

    const handleSelectConversation = (id: string) => {
        navigate(`/messages/${id}`);
    };

    return (
        <div id="mainChatroomsList">
            <Autocomplete
                options={otherUsers}
                value={selectedUser}
                onChange={(event, newValue) => {
                    setSelectedUser(newValue);
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
                        <img src={defaultProfileImage} alt="defaultProfileImage"/>
                        {chatRoom.name}
                        <LeaveChatroomModal chatroom={chatRoom} setRefreshConversations={setRefreshConversations}/>
                    </li>
                ))}
            </ul>
        </div>
    );
};