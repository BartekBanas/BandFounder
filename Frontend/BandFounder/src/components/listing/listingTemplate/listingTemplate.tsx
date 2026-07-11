import React, {useState} from 'react';
import {postListing} from "../../../api/listing";
import CreateListingCard from "../shared/CreateListingCard";
import ListingEditorModal from "../shared/ListingEditorModal";
import {useListingEditorState} from '../shared/useListingEditorState';
import {mantineErrorNotification} from '../../common/mantineNotification';

interface ListingTemplateProps {
    onListingCreated: () => void | Promise<void>;
}

const ListingTemplate: React.FC<ListingTemplateProps> = ({onListingCreated}) => {
    const [modalOpen, setModalOpen] = useState<boolean>(false);
    const editor = useListingEditorState({
        name: '',
        type: 'CollaborativeSong',
        genre: '',
        description: '',
        musicianSlots: [],
    });

    const handleListingClick = () => {
        setModalOpen(true);
    };

    const handlePostListing = async () => {
        try {
            await postListing(editor.toCreatePayload());
            setModalOpen(false);
            await onListingCreated();
        } catch (e) {
            console.error(e);
            mantineErrorNotification('Failed to create listing');
        }
    };

    return (
        <>
            <CreateListingCard onClick={handleListingClick}/>
            <ListingEditorModal
                open={modalOpen}
                title="Create Listing"
                submitLabel="Post"
                onClose={() => setModalOpen(false)}
                onSubmit={handlePostListing}
                listingName={editor.listingName}
                onListingNameChange={editor.setListingName}
                listingType={editor.listingType}
                onListingTypeChange={editor.setListingType}
                listingGenre={editor.listingGenre}
                onListingGenreChange={editor.setListingGenre}
                listingDescription={editor.listingDescription}
                onListingDescriptionChange={editor.setListingDescription}
                musicianSlots={editor.listingMusicianSlots}
                onAddRole={editor.addSlot}
                onDeleteRole={editor.deleteSlot}
                onEditMusicianRole={editor.editSlotRole}
                onEditSlotStatus={editor.editSlotStatus}
                genres={editor.genres}
                roles={editor.roles}
            />
        </>
    );
};

export default ListingTemplate;
