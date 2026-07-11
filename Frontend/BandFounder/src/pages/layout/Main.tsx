import React, {FC} from 'react';
import {Outlet} from "react-router-dom";
import {Content} from "../Content";
import {Header} from "./Header";
import '../../styles/theme.css';
import '../../styles/customScrollbar.css'

export const Main: FC = ({}) => {
    return (
        <div>
            <Header/>
            <Content>
                <Outlet/>
            </Content>
        </div>
    );
};