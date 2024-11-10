import React, {FC} from 'react';
import {Outlet} from "react-router-dom";
import {Content} from "./Content";
import {Header} from "./Header";
import {Menu} from "@mantine/core";
import classes = Menu.classes;

export const Main: FC = ({}) => {

    return (
        <div className={classes.rootContainer}>
            <Header/>
            <Content>
                <Outlet/>
            </Content>
        </div>
    );
};