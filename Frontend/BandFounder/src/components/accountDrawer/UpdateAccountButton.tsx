import {useDisclosure} from '@mantine/hooks';
import {Modal, Button, Group, Paper, Stack, TextInput, PasswordInput, Space} from '@mantine/core';
import React from "react";
import {useForm} from "@mantine/form";
import {updateAccountRequest} from "./api";
import {RegisterFormType} from "../register/api";
import {mantineErrorNotification, mantineSuccessNotification} from "../common/mantineNotification";

export function UpdateAccountButton() {
    const [opened, {close, open}] = useDisclosure(false);
    const form = useForm<RegisterFormType>({
        initialValues: {
            Name: '',
            Password: '',
            Email: '',
        },
    });

    const handleUpdateAccount = async () => {
        try {
            await updateAccountRequest(
                form.values.Name || null,
                form.values.Password || null,
                form.values.Email || null
            );
            mantineSuccessNotification("Account updated successfully");
        } catch (error) {
            mantineErrorNotification("Failed to update account");
        }

        close();
    };

    return (
        <>
            <Modal
                opened={opened}
                onClose={close}
                size="auto"
                styles={{
                    title: {
                        fontSize: '30px',
                        textAlign: 'center',
                        marginTop: '20px',
                    },
                }}
                title="Update your account"
            >
                <Space h="xl"/>
                <Paper shadow="sm" radius="md" p="lg" withBorder>
                    <form onSubmit={form.onSubmit(handleUpdateAccount)}>
                        <Stack>
                            <TextInput type="username" label="Username" {...form.getInputProps('Name')} />
                            <PasswordInput label="Password" withAsterisk {...form.getInputProps('Password')} />
                            <TextInput type="email" label="Email" {...form.getInputProps('Email')} />

                            <Space h="sm"/>

                            <Button type="submit">
                                Update Account
                            </Button>
                        </Stack>
                    </form>
                </Paper>
            </Modal>
            <Group>
                <Button color="blue" size="md" onClick={open}>
                    Update Account
                </Button>
            </Group>
        </>
    );
}