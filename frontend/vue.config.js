module.exports = {
  parallel: false,

  configureWebpack: {
    devServer: {
      compress: true,
      allowedHosts: "all",
      host: process.env.FRONTEND_HOST || "localhost",
      port: Number(process.env.FRONTEND_PORT || 8080),
    },

    resolve: {
      alias: {
        "@": require("path").resolve(__dirname, "src"),
      },
    },
  },
};
