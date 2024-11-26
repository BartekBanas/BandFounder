import React, {FC, useEffect} from 'react';
import '../styles/Header.css';
import {
    AppBar, Autocomplete,
    Box,
    CssBaseline,
    IconButton,
    TextField,
    ThemeProvider,
    Toolbar,
    Typography,
    InputAdornment
} from '@mui/material';
import {useNavigate} from 'react-router-dom';
import ChatIcon from '@mui/icons-material/Chat';
import SearchIcon from '@mui/icons-material/Search';
import {muiDarkTheme} from "../../assets/muiDarkTheme";
import {UtilityDrawer} from "../../components/accountDrawer/UtilityDrawer";
import {Account} from "../../types/Account";
import {getAccount, getAccounts} from "../../api/account";
import UserAvatar from "../../components/common/UserAvatar";
import {getUserId} from "../../hooks/authentication";
import {mantineErrorNotification} from "../../components/common/mantineNotification";

export const Header: FC = () => {
    const [users, setUsers] = React.useState<Account[]>([]);
    const [usernames, setUsernames] = React.useState<string[]>([]);
    const navigate = useNavigate();

    const handleProfileClick = () => {
        navigate('/profile');
    }

    const handleHomeClick = () => {
        navigate('/home');
    }

    const handleMessagesClick = () => {
        navigate('/messages');
    }

    const handleSearch = async (event: React.ChangeEvent<{}>, value: string | null) => {
        if (value) {
            try {
                const userId = users.find((user: Account) => user.name === value)?.id;
                const user = await getAccount(userId!);
                if (user) {
                    navigate(`/profile/${user.name}`);
                }
            } catch (e) {
                mantineErrorNotification('User not found');
                console.error('Error searching for user:', e);
            }
        }
    }

    useEffect(() => {
        const fetchUsers = async () => {
            try {
                const accounts = await getAccounts();
                setUsers(accounts);
                setUsernames(accounts.map((user: Account) => user.name));
            } catch (e) {
                console.error('Error fetching users:', e);
            }
        };

        fetchUsers();
    }, []);

    return (
        <ThemeProvider theme={muiDarkTheme}>
            <CssBaseline/>
            <Box id={'mainHeader'}>
                <AppBar position="static" color="transparent" sx={{display: 'flex', height: '80px'}}>
                    <Toolbar>
                        <div id={'leftSideOfMainHeader'}>
                            <Box
                                component="div"
                                sx={{display: 'flex', alignItems: 'center', mr: 2}}
                            >
                                <UtilityDrawer/>
                            </Box>
                            <Typography variant="h6" component="div" onClick={handleHomeClick} id={'appNameHeader'}>
                                Bandfounder
                            </Typography>
                        </div>

                        <div id={'rightSideOfMainHeader'}>
                            <Autocomplete
                                options={usernames}
                                filterOptions={(options, state) => {
                                    const filtered = options.filter((option) =>
                                        option.toLowerCase().includes(state.inputValue.toLowerCase())
                                    );
                                    return filtered.slice(0, 10);
                                }}
                                renderInput={(params) => (
                                    <TextField
                                        {...params}
                                        label="Search"
                                        InputProps={{
                                            ...params.InputProps,
                                            startAdornment: (
                                                <InputAdornment position="start">
                                                    <SearchIcon/>
                                                </InputAdornment>
                                            ),
                                        }}
                                    />
                                )}
                                freeSolo
                                disableClearable
                                onChange={handleSearch}
                                id={'searchBarMain'}
                            />

                            <IconButton color="inherit" onClick={handleMessagesClick}>
                                <ChatIcon/>
                            </IconButton>
                            <div style={{
                                display: 'flex',
                                flexDirection: 'column',
                                justifyContent: 'center',
                                alignItems: 'center'
                            }}>
                                <div onClick={handleProfileClick} style={{cursor: 'pointer'}}>
                                    <UserAvatar userId={getUserId()}/>
                                </div>
                            </div>
                        </div>
                    </Toolbar>
                </AppBar>
            </Box>
        </ThemeProvider>
    );
};