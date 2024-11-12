import React, {useState} from 'react';
import {Snackbar, Alert, AlertColor} from '@mui/material';
import {ThemeProvider} from '@mui/material/styles';
import {muiDarkTheme} from "../../assets/muiDarkTheme";

export function useMuiNotification() {
    const [open, setOpen] = useState(false);
    const [message, setMessage] = useState('');
    const [severity, setSeverity] = useState<AlertColor>('info');

    const showNotification = (message: string, severity: AlertColor) => {
        setMessage(message);
        setSeverity(severity);
        setOpen(true);
    };

    const closeNotification = () => setOpen(false);

    return {
        showSuccessMuiNotification: (message: string) => showNotification(message, 'success'),
        showInfoMuiNotification: (message: string) => showNotification(message, 'info'),
        showErrorMuiNotification: (message: string) => showNotification(message, 'error'),
        NotificationComponent: (
            <ThemeProvider theme={muiDarkTheme}>
                <Snackbar
                    open={open}
                    onClose={closeNotification}
                    anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
                >
                    <Alert
                        onClose={closeNotification}
                        severity={severity}
                    >
                        {message}
                    </Alert>
                </Snackbar>
            </ThemeProvider>
        ),
    };
}
