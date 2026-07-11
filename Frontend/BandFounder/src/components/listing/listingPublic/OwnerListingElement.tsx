import React, {useEffect, useState} from 'react';
import '../shared/listingShared.css';
import {Button} from '@mui/material';
import {Listing} from "../../../types/Listing";
import {contactListingOwner} from "../../../api/listing";
import {getDirectChatroomWithUser} from "../../../api/chatroom";
import {mantineErrorNotification, mantineInformationNotification} from "../../common/mantineNotification";
import {getUserId} from "../../../hooks/authentication";
import {Account} from "../../../types/Account";
import {getAccount} from "../../../api/account";
import InteractiveUserAvatar from "../../common/InteractiveUserAvatar";

interface OwnerListingElementProps {
    listing: Listing;
    onOwnerLoaded?: (name: string) => void;
}

const OwnerListingElement = ({listing, onOwnerLoaded}: OwnerListingElementProps) => {
    const [owner, setOwner] = useState<Account | undefined>(undefined);

    useEffect(() => {
        if (listing?.ownerId) {
            getAccount(listing.ownerId).then((data) => {
                setOwner(data);
                if (data?.name) {
                    onOwnerLoaded?.(data.name);
                }
            }).catch((error) => {
                console.error(error);
            });
        }
    }, [listing, onOwnerLoaded]);

    const handleMessageListingOwner = async () => {
        try {
            if (listing.ownerId === getUserId()) {
                mantineInformationNotification("I don't think you need to message yourself");
                return;
            }

            if (listing?.ownerId) {
                const response = await contactListingOwner(listing.id);

                if (response) {
                    window.location.href = '/messages/' + response.id;
                } else {
                    const chatroom = await getDirectChatroomWithUser(listing.ownerId);
                    if (chatroom?.id) {
                        window.location.href = '/messages/' + chatroom.id;
                    } else {
                        throw new Error('Failed to find chatroom with user ' + listing.ownerId);
                    }
                }
            }
        } catch (error) {
            console.error(error);
            mantineErrorNotification("Failed to contact the listing's owner");
        }
    };

    const isOwnListing = listing.ownerId === getUserId();

    const messageOwnerAction = !isOwnListing ? (
        <Button
            variant="contained"
            color="primary"
            onClick={handleMessageListingOwner}
            sx={{flex: 1}}
        >
            Message Owner
        </Button>
    ) : undefined;

    return (
        <InteractiveUserAvatar
            userId={listing.ownerId}
            name={owner?.name}
            size={40}
            showName={false}
            extraActions={messageOwnerAction}
        />
    );
};

export default OwnerListingElement;
