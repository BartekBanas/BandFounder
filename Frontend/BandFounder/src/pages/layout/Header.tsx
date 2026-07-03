import React, {FC, useEffect} from 'react';
import '../styles/Header.css';
import {
    AppBar,
    Autocomplete,
    Box,
    CssBaseline,
    IconButton,
    TextField,
    ThemeProvider,
    Toolbar,
    InputAdornment
} from '@mui/material';
import {useNavigate} from 'react-router-dom';
import ChatIcon from '@mui/icons-material/Chat';
import SearchIcon from '@mui/icons-material/Search';
import GraphicEqIcon from '@mui/icons-material/GraphicEq';
import {muiDarkTheme} from "../../styles/muiDarkTheme";
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
        window.location.href = '/home';
    }

    const handleMessagesClick = () => {
        navigate('/messages');
    }

    const handleSearch = async (_event: React.SyntheticEvent, value: string | null) => {
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
            <Box id="mainHeader">
                <AppBar position="static" color="transparent" elevation={0} sx={{background: 'transparent'}}>
                    <Toolbar disableGutters sx={{minHeight: '80px'}}>
                        <div className="header-toolbar">
                            <div className="header-toolbar__left">
                                <UtilityDrawer/>
                                <div id="appNameHeader" onClick={handleHomeClick}>
                                    <span className="header-logo-icon">
                                        <GraphicEqIcon/>
                                    </span>
                                    <span>Bandfounder</span>
                                </div>
                            </div>

                            <div className="header-toolbar__center">
                                <Autocomplete
                                    options={usernames}
                                    filterOptions={(options, state) => {
                                        return options
                                            .filter((option) =>
                                                option.toLowerCase().includes(state.inputValue.toLowerCase())
                                            )
                                            .slice(0, 10);
                                    }}
                                    renderInput={(params) => (
                                        <TextField
                                            {...params}
                                            placeholder="Search musicians..."
                                            size="small"
                                            InputProps={{
                                                ...params.InputProps,
                                                startAdornment: (
                                                    <InputAdornment position="start">
                                                        <SearchIcon sx={{color: 'var(--text-muted)'}}/>
                                                    </InputAdornment>
                                                ),
                                            }}
                                        />
                                    )}
                                    freeSolo
                                    disableClearable
                                    onChange={handleSearch}
                                    id="searchBarMain"
                                />
                            </div>

                            <div className="header-toolbar__right">
                                <IconButton color="inherit" onClick={handleMessagesClick} aria-label="Messages">
                                    <ChatIcon/>
                                </IconButton>
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
