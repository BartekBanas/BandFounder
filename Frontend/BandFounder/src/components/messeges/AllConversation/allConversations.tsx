import { FC, useState, useEffect } from "react";
import { ChatRoom } from "../../../types/ChatRoom";
import { getAllConversations, getAllUsers, createNewChatroom } from "./api"; // Assuming this function exists in your API
import { Autocomplete, TextField } from "@mui/material";
import Cookies from "universal-cookie";
import { getUserById } from "../../common/frequentlyUsed";

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

    useEffect(() => {
        // console.log(chatRooms);
    }, [chatRooms]);

    const handleCreateChatroom = async (userName: string | null) => {
        if (!userName) return;
        try {
            const newChatRoom = await createNewChatroom(userName);
            setChatRooms((prev) => [...prev, newChatRoom]); // Add the new chatroom to the list
        } catch (error) {
            console.error("Error creating chatroom:", error);
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
                    <li key={chatRoom.id}>{chatRoom.name}</li>
                ))}
            </ul>
        </div>
    );
};