// src/components/messeges/AllConversation/allConversations.tsx
import { FC, useState, useEffect } from "react";
import { ChatRoom } from "../../../types/ChatRoom";
import { getAllConversations, getAllUsers, createNewChatroom, leaveChatroom } from "./api";
import { Autocomplete, IconButton, TextField } from "@mui/material";
import DeleteIcon from "@mui/icons-material/Delete";
import {useNavigate} from "react-router-dom";

interface AllConversationsProps {
    onSelectConversation: (id: string) => void;
}

export const AllConversations: FC<AllConversationsProps> = ({ onSelectConversation }) => {
    const [chatRooms, setChatRooms] = useState<ChatRoom[]>([]);
    const [users, setUsers] = useState<string[]>([]);
    const [selectedUser, setSelectedUser] = useState<string | null>(null);

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

    const handleCreateChatroom = async (userName: string | null) => {
        if (!userName) return;
        try {
            const newChatRoom = await createNewChatroom(userName);
            fetchConversations();
        } catch (error) {
            console.error("Error creating chatroom:", error);
        }
    };

    const handleLeaveChatroom = async (chatRoomId: string) => {
        try {
            console.log(`Leaving chatroom with id: ${chatRoomId}`);
            await leaveChatroom(chatRoomId);
            fetchConversations();
        } catch (error) {
            console.error("Error leaving chatroom:", error);
        }
    };

    const navigate = useNavigate();
    const handleSelectConversation = (id: string) => {
        navigate(`/messages/${id}`);
    };

    return (
        <div id="main">
            <Autocomplete
                options={users}
                value={selectedUser}
                onChange={(event, newValue) => {
                    setSelectedUser(newValue);
                    handleCreateChatroom(newValue);
                }}
                renderInput={(params) => <TextField {...params} label="Search Users" variant="outlined" />}
                style={{ margin: "20px", maxWidth: "300px" }}
            />
            <h1>All open conversations</h1>

            <ul>
                {chatRooms.map((chatRoom) => (
                    <li key={chatRoom.id} onClick={() => handleSelectConversation(chatRoom.id)}>
                        {chatRoom.name}
                        <IconButton onClick={() => handleLeaveChatroom(chatRoom.id)}>
                            <DeleteIcon />
                        </IconButton>
                    </li>
                ))}
            </ul>
        </div>
    );
};
