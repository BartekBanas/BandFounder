// src/pages/MessagesPage.tsx
import { FC, useState } from "react";
import { AllConversations } from "../components/messeges/AllConversation/allConversations";
import { SelectedConversation } from "../components/messeges/SelectedConversation/selectedConversation";
import {muiDarkTheme} from "../assets/muiDarkTheme";
import {ThemeProvider} from "@mui/material";
import './styles/MessagesPageMain.css'

interface MessagesPageMainProps {}

export const MessagesPageMain: FC<MessagesPageMainProps> = ({}) => {
    const [selectedConversationId, setSelectedConversationId] = useState<string>('');

    return (
        <div id="messagesPageMain">
            <AllConversations onSelectConversation={setSelectedConversationId} />
        </div>
    );
};