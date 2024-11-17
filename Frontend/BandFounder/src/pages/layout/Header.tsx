import React, {FC} from 'react';
import '../styles/Header.css';
import {removeAuthToken, removeUserId} from "../../hooks/authentication";
import {
    AppBar,
    Box,
    Button,
    CssBaseline,
    IconButton,
    ThemeProvider,
    Toolbar,
    Typography
} from '@mui/material';
import defaultProfileImage from '../../assets/defaultProfileImage.jpg';
import { useNavigate } from 'react-router-dom';
import ChatIcon from '@mui/icons-material/Chat';
import {muiDarkTheme} from "../../assets/muiDarkTheme";
import {UtilityDrawer} from "../../components/accountDrawer/UtilityDrawer";

export const Header: FC = () => {
    const navigate = useNavigate();

    const handleLogout = () => {
        removeAuthToken();
        removeUserId();
        window.location.reload();
    }

    const handleProfileClick = () => {
        navigate('/profile');
    }

    const handleHomeClick = () => {
        navigate('/home');
    }

    const handleMessagesClick = () => {
        navigate('/messages');
    }

    return (
        <ThemeProvider theme={muiDarkTheme}>
            <CssBaseline/>
            <Box id={'mainHeader'}>
                <AppBar position="static" color="transparent" sx={{display: 'flex', height: '80px'}}>
                    <Toolbar>
                        <Box
                            component="div"
                            sx={{ display: 'flex', alignItems: 'center', mr: 2}}
                        >
                            <UtilityDrawer />
                        </Box>
                        <Typography variant="h6" component="div" onClick={handleHomeClick} id={'appNameHeader'}>
                            Bandfounder
                        </Typography>
                        <IconButton color="inherit" onClick={handleMessagesClick}>
                            <ChatIcon />
                        </IconButton>
                        <div style={{display:'flex', flexDirection:'column', justifyContent:'center', alignItems:'center'}}>
                            <div onClick={handleProfileClick} style={{cursor: 'pointer'}}>
                                <img src={defaultProfileImage} alt="profile" className="profile-image"/>
                            </div>
                            <Button color="primary" size="large" onClick={handleLogout}>
                                Logout
                            </Button>
                        </div>
                    </Toolbar>
                </AppBar>
            </Box>
        </ThemeProvider>
    );
};