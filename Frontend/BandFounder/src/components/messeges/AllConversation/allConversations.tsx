import { FC, useState, useEffect } from "react";
import { ChatRoom } from "../../../types/ChatRoom";
import { getAllConversations, getAllUsers, createNewChatroom, leaveChatroom } from "./api";
import {Autocomplete, CircularProgress, IconButton, TextField} from "@mui/material";
import { useNavigate } from "react-router-dom";
import './styles.css'
import defaultProfileImage from '../../../assets/defaultProfileImage.jpg';
import './../../../assets/CustomScrollbar.css'
import CancelPresentationIcon from '@mui/icons-material/CancelPresentation';

interface AllConversationsProps {
    onSelectConversation: (id: string) => void;
}

export const AllConversations: FC<AllConversationsProps> = ({ onSelectConversation }) => {
    const [chatRooms, setChatRooms] = useState<ChatRoom[]>([]);
    const [users, setUsers] = useState<string[]>([]);
    const [selectedUser, setSelectedUser] = useState<string | null>(null);
    const navigate = useNavigate();

    useEffect(() => {
        const fetchConversations = async () => {
            const conversations = await getAllConversations();
            if (conversations) {
                setChatRooms(conversations);
            }
        };
        const fetchUsers = async () => {
            const users = await getAllUsers();
            setUsers(users.map((user: any) => user.name));
        };

        fetchConversations();
        fetchUsers();
    }, []);

    const fetchConversations = async () => {
        const conversations = await getAllConversations();
        if (conversations) {
            setChatRooms(conversations);
        }
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
            const newChatRoom = await createNewChatroom(userName);
            fetchConversations();
        } catch (error: any) {
            if (error.status === 409) {
                const chatroomId = await checkIdOfChatroom(userName);
                window.location.href = `/messages/${chatroomId}`;
            } else {
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
                options={users}
                value={selectedUser}
                onChange={(event, newValue) => {
                    setSelectedUser(newValue);
                    handleCreateChatroom(newValue);
                }}
                renderInput={(params) => <TextField {...params} label="Search Users" variant="outlined" />}
                style={{ margin: "20px", maxWidth: "100%" }}
            />
            <div style={{display: 'flex', justifyContent: 'center', alignItems:'center'}}>
                {
                    chatRooms.length > 0 ? <h2>Open Conversations</h2> : <CircularProgress size={30} sx={{marginBottom:'20px'}}/>
                }
            </div>
            <ul id={'openConversationsList'} className={'custom-scrollbar'}>
                {chatRooms.map((chatRoom) => (
                    <li className={'singleConversationShrotcut'} key={chatRoom.id} onClick={() => handleSelectConversation(chatRoom.id)}>
                        <img src={defaultProfileImage} alt="defaultProfileImage" />
                        {chatRoom.name}
                        <IconButton onClick={() => handleLeaveChatroom(chatRoom.id)}>
                            <CancelPresentationIcon />
                        </IconButton>
                    </li>
                ))}
            </ul>
        </div>
    );
};