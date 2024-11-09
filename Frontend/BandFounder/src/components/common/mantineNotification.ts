import {showNotification} from "@mantine/notifications";
import notificationStyles from "../../styles/notificationStyles";
import '@mantine/notifications/styles.css';

export function mantineSuccessNotification(message: string): void {
    showNotification({
        color: 'green',
        title: 'Success',
        message: message,
        styles: notificationStyles,
    });
}

export function mantineInformationNotification(message: string): void {
    showNotification({
        color: 'blue',
        message: message,
        styles: notificationStyles,
    });
}

export function mantineErrorNotification(message: string): void {
    showNotification({
        color: 'red',
        title: 'Error',
        message: message,
        styles: notificationStyles,
    });
}