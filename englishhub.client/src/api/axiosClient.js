import axios from "axios";
import authApi from "./authApi";

// Kiểm tra biến môi trường
if (!import.meta.env.VITE_API_BASE_URL) {
    throw new Error("API base URL is not defined in .env");
}

const axiosClient = axios.create({
    baseURL: import.meta.env.VITE_API_BASE_URL,
    headers: {
        "Content-Type": "application/json",
    },
    timeout: 10000,
});

// Gắn token vào request
axiosClient.interceptors.request.use(
    (config) => {
        const token = localStorage.getItem("token");
        if (token) {
            config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
    },
    (error) => Promise.reject(error)
);

// Biến kiểm soát refresh token
let isRefreshing = false;
let failedQueue = [];

const processQueue = (error, token = null) => {
    failedQueue.forEach((prom) => {
        if (error) {
            prom.reject(error);
        } else {
            prom.resolve(token);
        }
    });
    failedQueue = [];
};

// Xử lý response
axiosClient.interceptors.response.use(
    (response) => response.data,
    async (error) => {
        const originalRequest = error.config;

        // Nếu token hết hạn
        if (error.response?.status === 401 && !originalRequest._retry) {
            if (isRefreshing) {
                // Đợi token mới
                return new Promise((resolve, reject) => {
                    failedQueue.push({ resolve, reject });
                })
                    .then((token) => {
                        originalRequest.headers.Authorization = "Bearer " + token;
                        return axiosClient(originalRequest);
                    })
                    .catch((err) => Promise.reject(err));
            }

            originalRequest._retry = true;
            isRefreshing = true;

            try {
                // Refresh token
                const refreshToken = localStorage.getItem("refreshToken");
                const res = await authApi.refreshToken({ refreshToken });

                const newToken = res.token;
                localStorage.setItem("token", newToken);

                axiosClient.defaults.headers.Authorization = "Bearer " + newToken;
                processQueue(null, newToken);

                return axiosClient(originalRequest);
            } catch (err) {
                processQueue(err, null);
                localStorage.removeItem("token");
                localStorage.removeItem("refreshToken");

                // Điều hướng về login
                window.location.href = "/login";
                return Promise.reject(err);
            } finally {
                isRefreshing = false;
            }
        }

        return Promise.reject(error);
    }
);

export default axiosClient;
