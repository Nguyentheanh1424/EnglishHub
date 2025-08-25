import axiosClient from "./axiosClient";

const authApi = {
    // Đăng ký
    registor: (data) => {
        // data: { username, email, password}
        return axiosClient.post("/auth/register", data);
    },

    // Đăng nhập
    login: (data) => {
        // data: { username, password }
        return axiosClient.post("/auth/login", data);
    },

    // Refresh token
    refreshToken: (data) => {
        // data: {userId, refreshToken }
        return axiosClient.post("/auth/refresh", data);
    },

    // Đăng xuất 
    logout: (data) => {
        // data: {refreshToken }
        return axiosClient.post("/auth/logout", data);
    },
};

export default authApi;
