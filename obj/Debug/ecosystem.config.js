module.exports = {
  apps: [
    {
      name: "scp1356-status",
      script: "./server.js",
      instances: 1,
      exec_mode: "fork",
      watch: false,
      max_memory_restart: "300M",
      env: {
        NODE_ENV: "production",
        PORT: 3000,
        SCP1356_API_TOKEN: "SUPER_SECRET_TOKEN_HERE"
      }
    }
  ]
};