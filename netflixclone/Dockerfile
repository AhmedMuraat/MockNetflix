# Stage 1: Build the React app
FROM node:20 AS build

# Set the working directory
WORKDIR /app

# Copy the package.json and package-lock.json to the working directory
COPY package.json package-lock.json ./

# Install dependencies
RUN npm install

# Copy the rest of the application code to the working directory
COPY . .

# Build the React app
RUN npm run build

# Stage 2: Serve the React app using nginx
FROM nginx:latest

# Copy the build output from the previous stage to the nginx html directory
COPY --from=build /app/build /usr/share/nginx/html

# Expose port 80
EXPOSE 80

# Start nginx
CMD ["nginx", "-g", "daemon off;"]
