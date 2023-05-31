#[macro_use]
extern crate rocket;

mod entities {
    pub mod poll;
}

use entities::poll::Poll;

#[get("/")]
fn index() -> &'static str {
    "Hello, world!"
}

#[rocket::main]
async fn main() -> Result<(), rocket::Error> {
    let _rocket = rocket::build().mount("/", routes![index]).launch().await?;
    Ok(())
}
