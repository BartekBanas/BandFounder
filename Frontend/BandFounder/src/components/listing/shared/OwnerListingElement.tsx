import React, {useEffect, useState} from 'react';
import {useNavigate} from 'react-router-dom';
import {Button} from '@mui/material';
import {Listing} from '../../../types/Listing';
import {Account} from '../../../types/Account';
import {contactListingOwner} from '../../../api/listing';
import {openDirectChatroomWithFallback} from '../../../api/chatroom';
import {mantineErrorNotification, mantineInformationNotification} from '../../common/mantineNotification';
import {getUserId} from '../../../hooks/authentication';
import {getAccount} from '../../../api/account';
import InteractiveUserAvatar from '../../common/InteractiveUserAvatar';

interface OwnerListingElementProps {
    listing: Listing;
    owner?: Account;
    onOwnerLoaded?: (name: string) => void;
}

const OwnerListingElement = ({listing, owner: preloadedOwner, onOwnerLoaded}: OwnerListingElementProps) => {
    const navigate = useNavigate();
    const [fetchedOwner, setFetchedOwner] = useState<Account | undefined>(undefined);
    const owner = preloadedOwner ?? fetchedOwner;

    useEffect(() => {
        if (preloadedOwner) {
            onOwnerLoaded?.(preloadedOwner.name);
            return;
        }

        if (listing?.ownerId) {
            getAccount(listing.ownerId).then((data) => {
                setFetchedOwner(data);
                if (data?.name) {
                    onOwnerLoaded?.(data.name);
                }
            }).catch((error) => {
                console.error(error);
            });
        }
    }, [listing, onOwnerLoaded, preloadedOwner]);

    const handleMessageListingOwner = async () => {
        try {
            if (listing.ownerId === getUserId()) {
                mantineInformationNotification("I don't think you need to message yourself");
                return;
            }

            if (listing?.ownerId) {
                await openDirectChatroomWithFallback(
                    listing.ownerId,
                    () => contactListingOwner(listing.id),
                    (chatroomId) => navigate(`/messages/${chatroomId}`)
                );
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
