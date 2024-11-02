import Cookies from 'universal-cookie';

const useAccountAuthorization = () => {
    const cookies = new Cookies();
    return cookies.get('auth_token') !== undefined;
};

export default useAccountAuthorization;