import React, {FC} from 'react';
import {Button, MantineProvider, Paper, Text} from '@mantine/core';
import {Link} from "react-router-dom";
import {UtilityDrawer} from '../components/accountDrawer/UtilityDrawer';
import {removeAuthToken} from "../components/accountDrawer/api";

export const Header: FC = () => {
    return (
        <header style={{display: 'flex', justifyContent: 'center'}}>
            <MantineProvider defaultColorScheme="dark">
                <Paper
                    shadow="sm"
                    radius="md"
                    p="lg"
                    withBorder
                    style={{
                        width: '100%',
                        display: 'flex',
                        justifyContent: 'space-between',
                        alignItems: 'center',
                        marginBottom: '15px',
                    }}
                >
                    <UtilityDrawer/>

                    <div style={{flex: '1', textAlign: 'center'}}>
                        <Text color={'#D5D7E0'}>
                            Bandfounder
                        </Text>
                    </div>

                    <Link to="/login" style={{textDecoration: 'none'}}>
                        <Button variant="light" onClick={removeAuthToken}>
                            Logout
                        </Button>
                    </Link>
                </Paper>
            </MantineProvider>
        </header>
    );
};