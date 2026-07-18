import React, {FC} from 'react';
import {Outlet} from "react-router-dom";
import {Content} from "../Content";
import {Header} from "./Header";
import {muiDarkTheme} from "../../styles/muiDarkTheme";
import {ThemeProvider} from "@mui/material";
import {UnreadMessagesProvider} from "../../hooks/useUnreadMessages";
import '../../styles/theme.css';
import '../../styles/customScrollbar.css'

export const Main: FC = ({}) => {
    return (
        <ThemeProvider theme={muiDarkTheme}>
            <UnreadMessagesProvider>
                <div>
                    <Header/>
                    <Content>
                        <Outlet/>
                    </Content>
                </div>
            </UnreadMessagesProvider>
        </ThemeProvider>
    );
};