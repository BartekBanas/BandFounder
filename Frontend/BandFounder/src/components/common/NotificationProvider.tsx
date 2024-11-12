import React, { createContext, useContext } from 'react';
import {useMuiNotification} from "./useMuiNotification";

const NotificationContext = createContext<{
    showSuccessMuiNotification: (message: string) => void;
    showInfoMuiNotification: (message: string) => void;
    showErrorMuiNotification: (message: string) => void;
} | null>(null);

export const NotificationProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    const { showSuccessMuiNotification, showInfoMuiNotification, showErrorMuiNotification, NotificationComponent } = useMuiNotification();

    return (
        <NotificationContext.Provider value={{ showSuccessMuiNotification, showInfoMuiNotification, showErrorMuiNotification }}>
            {NotificationComponent}
            {children}
        </NotificationContext.Provider>
    );
};

export const useNotification = () => {
    const context = useContext(NotificationContext);
    if (!context) {
        throw new Error('useNotification must be used within a NotificationProvider');
    }
    return context;
};
