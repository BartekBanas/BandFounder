import React, {FC} from 'react';
import {Button} from '@mantine/core';
import {Link} from "react-router-dom";
import {UtilityDrawer} from '../../components/accountDrawer/UtilityDrawer';
import '../styles/Header.css';
import {removeAuthToken, removeUserId} from "../../hooks/authentication";

export const Header: FC = () => {
    const handleLogout = () => {
        removeAuthToken();
        removeUserId();
    }

    return (
        <div id={'mainHeader'}>
            <UtilityDrawer/>

            <p id={'header-text'}>BandFounder</p>

            <Link to="/login">
                <Button variant="light" onClick={handleLogout}>
                    Logout
                </Button>
            </Link>
        </div>
    );
};

export {};