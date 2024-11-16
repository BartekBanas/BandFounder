import { FC, useState, useEffect } from "react";
import { ChatRoom } from "../../../types/ChatRoom";
import {getAllConversations, getAllUsers, createNewChatroom, leaveChatroom} from "./api"; // Assuming this function exists in your API
import {Autocomplete, IconButton, TextField} from "@mui/material";
import Cookies from "universal-cookie";
import { getUserById } from "../../common/frequentlyUsed";
import DeleteIcon from "@mui/icons-material/Delete";

interface AllConversationsProps {}

export const AllConversations: FC<AllConversationsProps> = ({}) => {
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
            setUsers(users.map((user: any) => user.name)); // Assuming user object has a 'name' property
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

    useEffect(() => {
        // console.log(chatRooms);
    }, [chatRooms]);

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

    return (
        <div id="main">
            <Autocomplete
                options={users}
                value={selectedUser}
                onChange={(event, newValue) => {
                    setSelectedUser(newValue);
                    handleCreateChatroom(newValue); // Trigger chatroom creation
                }}
                renderInput={(params) => <TextField {...params} label="Search Users" variant="outlined" />}
                style={{ margin: "20px", maxWidth: "300px" }}
            />
            <h1>All open conversations</h1>

            <ul>
                {chatRooms.map((chatRoom) => (
                    <li key={chatRoom.id}>
                        {chatRoom.name}
                        <IconButton onClick={() => handleLeaveChatroom(chatRoom.id)}>
                            <DeleteIcon/>
                        </IconButton>
                    </li>
                ))}
            </ul>
        </div>
    );
};