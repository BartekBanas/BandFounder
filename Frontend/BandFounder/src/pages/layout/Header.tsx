import React, {FC} from 'react';
import {Button, MantineProvider, Paper, Text} from '@mantine/core';
import {Link} from "react-router-dom";
import {UtilityDrawer} from '../../components/accountDrawer/UtilityDrawer';
import {removeAuthToken} from "../../components/accountDrawer/api";
import '../styles/Header.css';

export const Header: FC = () => {
    return (
        <div id={'mainHeader'}>
            <UtilityDrawer/>

            <p id={'header-text'}>BandFounder</p>

            <Link to="/login">
                <Button variant="light" onClick={removeAuthToken}>
                    Logout
                </Button>
            </Link>
        </div>
    );
};