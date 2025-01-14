import React, {FC} from 'react';
import {Outlet} from "react-router-dom";
import {Content} from "../Content";
import {Header} from "./Header";
import {Menu} from "@mantine/core";
import classes = Menu.classes;
import {muiDarkTheme} from "../../styles/muiDarkTheme";
import {ThemeProvider} from "@mui/material";
import '../../styles/customScrollbar.css'

export const Main: FC = ({}) => {

    return (
        <ThemeProvider theme={muiDarkTheme}>
        <div>
            <Header/>
            <Content>
                <Outlet/>
            </Content>
        </div>
        </ThemeProvider>
    );
};